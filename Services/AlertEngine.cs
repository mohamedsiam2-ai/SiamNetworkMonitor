using System.IO;
using System.Text.Json;
using SiamNetworkMonitor.Alerts;

namespace SiamNetworkMonitor.Services;

public sealed class AlertEngine : IAlertEvaluator, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<(Guid RuleId, string AppKey), AlertEvaluationState> _states = new();
    private List<AlertRule> _rules;
    private readonly List<AlertEvent> _history = new();
    private readonly string _historyPath;
    private readonly HashSet<string> _excludedProcesses;
    private readonly HashSet<string> _excludedPaths;
    private readonly int _retentionDays;
    private readonly int _maximumEvents;

    public AlertEngine(IEnumerable<AlertRule> rules, IEnumerable<string>? excludedProcessNames = null,
        IEnumerable<string>? excludedExecutablePaths = null, int retentionDays = 90, int maximumEvents = 5000,
        string? historyPath = null)
    {
        _rules = rules.Where(r => r is not null).ToList();
        _excludedProcesses = new(excludedProcessNames ?? [], StringComparer.OrdinalIgnoreCase);
        _excludedPaths = new((excludedExecutablePaths ?? []).Select(NormalizePath), StringComparer.OrdinalIgnoreCase);
        _retentionDays = Math.Clamp(retentionDays, 1, 3650); _maximumEvents = Math.Clamp(maximumEvents, 100, 100000);
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SiamNetworkMonitor");
        Directory.CreateDirectory(folder); _historyPath = historyPath ?? Path.Combine(folder, "alert-history.json"); LoadHistory();
    }

    public IReadOnlyList<AlertEvent> History { get { lock (_gate) return _history.OrderByDescending(x => x.TriggeredUtc).ToList(); } }
    public void ReplaceRules(IEnumerable<AlertRule> rules){lock(_gate){_rules=rules.ToList();_states.Clear();}}

    public IReadOnlyList<AlertEvent> Evaluate(IReadOnlyCollection<ApplicationTrafficSnapshot> applications, DateTime timestampUtc)
    {
        if (applications is null) throw new ArgumentNullException(nameof(applications));
        timestampUtc = timestampUtc.Kind == DateTimeKind.Utc ? timestampUtc : timestampUtc.ToUniversalTime();
        var triggered = new List<AlertEvent>();
        lock (_gate)
        {
            var snapshots = applications.Where(IsEligible).ToList();
            foreach (var rule in _rules.Where(r => r.Enabled))
            {
                if (rule.Type == AlertRuleType.DailyTotalUsage)
                    EvaluateDailyTotal(rule, snapshots, timestampUtc, triggered);
                else
                    foreach (var app in snapshots.Where(a => Matches(rule, a))) EvaluateApplication(rule, app, timestampUtc, triggered);
            }
            if (triggered.Count > 0) { _history.AddRange(triggered); Prune(timestampUtc); SaveUnsafe(); }
        }
        return triggered;
    }

    private void EvaluateDailyTotal(AlertRule rule, List<ApplicationTrafficSnapshot> apps, DateTime now, List<AlertEvent> output)
    {
        var observed = apps.Sum(a => a.TodayTotalKB); var representative = apps.OrderByDescending(a => a.TodayTotalKB).FirstOrDefault() ?? new ApplicationTrafficSnapshot { ProcessName = "All applications", Description = "All applications" };
        EvaluateValue(rule, representative, "__daily_total__", observed, rule.ThresholdKB, "KB", now, output, false);
    }

    private void EvaluateApplication(AlertRule rule, ApplicationTrafficSnapshot app, DateTime now, List<AlertEvent> output)
    {
        var key = ApplicationKey(app); double observed, threshold; string unit; bool sustained = false;
        switch (rule.Type)
        {
            case AlertRuleType.ApplicationSessionUsage: observed = app.SessionTotalKB; threshold = rule.ThresholdKB; unit = "KB"; break;
            case AlertRuleType.UnsignedApplicationUsage:
                if (app.IsSigned == true || (app.IsSigned is null && !rule.IncludeUnknownSignature)) { ResetCrossing(rule.Id, key); return; }
                observed = app.SessionTotalKB; threshold = rule.ThresholdKB; unit = "KB"; break;
            case AlertRuleType.SustainedDownloadSpeed: observed = app.DownloadKBps; threshold = rule.ThresholdKBps; unit = "KB/s"; sustained = true; break;
            case AlertRuleType.SustainedUploadSpeed: observed = app.UploadKBps; threshold = rule.ThresholdKBps; unit = "KB/s"; sustained = true; break;
            case AlertRuleType.UploadHeavyActivity:
                if (rule.SustainedSeconds > 0 && rule.ThresholdKBps > 0) { observed = app.UploadKBps; threshold = rule.ThresholdKBps; unit = "KB/s"; sustained = true; }
                else { observed = app.SessionUploadKB; threshold = rule.ThresholdKB; unit = "KB"; }
                break;
            default: return;
        }
        if (app.SessionTotalKB < Math.Max(0, rule.MinimumTrafficKB)) { ResetCrossing(rule.Id, key); return; }
        EvaluateValue(rule, app, key, observed, threshold, unit, now, output, sustained);
    }

    private void EvaluateValue(AlertRule rule, ApplicationTrafficSnapshot app, string appKey, double observed, double threshold, string unit, DateTime now, List<AlertEvent> output, bool sustained)
    {
        if (threshold <= 0 || observed < threshold) { ResetCrossing(rule.Id, appKey); return; }
        var key = (rule.Id, appKey); if (!_states.TryGetValue(key, out var state)) _states[key] = state = new();
        state.PeakObservedValue = Math.Max(state.PeakObservedValue, observed); state.IsCurrentlyAboveThreshold = true; state.ThresholdStartedUtc ??= now;
        var duration = Math.Max(0, (int)Math.Floor((now - state.ThresholdStartedUtc.Value).TotalSeconds));
        if (sustained && duration < Math.Max(1, rule.SustainedSeconds)) return;
        if (state.TriggeredForCurrentCrossing || InCooldown(state, rule, now)) return;
        var evt = CreateEvent(rule, app, observed, threshold, unit, sustained ? duration : 0, now);
        output.Add(evt); state.LastTriggeredUtc = now; state.TriggeredForCurrentCrossing = true;
    }

    private static AlertEvent CreateEvent(AlertRule rule, ApplicationTrafficSnapshot app, double observed, double threshold, string unit, int sustainedSeconds, DateTime now)
    {
        var signature = app.IsSigned switch { true => "Signed", false => "Unsigned", null => "Unknown" };
        var durationText = sustainedSeconds > 0 ? $" for {sustainedSeconds} seconds" : "";
        return new AlertEvent { RuleId=rule.Id,RuleName=rule.Name,RuleType=rule.Type,Severity=rule.Severity,TriggeredUtc=now,
            ProcessId=app.ProcessId,ProcessName=app.ProcessName,Description=app.Description,Publisher=app.Publisher,ExecutablePath=app.ExecutablePath,IsSigned=app.IsSigned,
            ObservedValue=observed,ThresholdValue=threshold,Unit=unit,SustainedSeconds=sustainedSeconds,DownloadKBps=app.DownloadKBps,UploadKBps=app.UploadKBps,
            SessionDownloadKB=app.SessionDownloadKB,SessionUploadKB=app.SessionUploadKB,TodayDownloadKB=app.TodayDownloadKB,TodayUploadKB=app.TodayUploadKB,
            EvidenceSummary=$"{DisplayName(app)} reached {observed:N2} {unit}{durationText}. Configured threshold: {threshold:N2} {unit}. Publisher: {NonEmpty(app.Publisher,"Unavailable")}. Signature: {signature}." };
    }

    private bool IsEligible(ApplicationTrafficSnapshot app) => !_excludedProcesses.Contains(app.ProcessName) && !_excludedPaths.Contains(NormalizePath(app.ExecutablePath));
    private static bool Matches(AlertRule rule, ApplicationTrafficSnapshot app)
    {
        if (rule.Scope == AlertScope.AllApplications) return true;
        if (!string.IsNullOrWhiteSpace(rule.ExecutablePath) && string.Equals(NormalizePath(rule.ExecutablePath), NormalizePath(app.ExecutablePath), StringComparison.OrdinalIgnoreCase)) return true;
        return !string.IsNullOrWhiteSpace(rule.ProcessName) && string.Equals(rule.ProcessName, app.ProcessName, StringComparison.OrdinalIgnoreCase);
    }
    private static bool InCooldown(AlertEvaluationState state, AlertRule rule, DateTime now) => state.LastTriggeredUtc is DateTime last && now-last < TimeSpan.FromMinutes(Math.Max(0,rule.CooldownMinutes));
    private void ResetCrossing(Guid ruleId,string appKey){if(_states.TryGetValue((ruleId,appKey),out var s)){s.ThresholdStartedUtc=null;s.PeakObservedValue=0;s.IsCurrentlyAboveThreshold=false;s.TriggeredForCurrentCrossing=false;}}
    private static string ApplicationKey(ApplicationTrafficSnapshot app)=>!string.IsNullOrWhiteSpace(app.ExecutablePath)?NormalizePath(app.ExecutablePath):app.ProcessName.Trim().ToLowerInvariant();
    private static string NormalizePath(string? path){if(string.IsNullOrWhiteSpace(path))return "";try{return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();}catch{return path.Trim().ToLowerInvariant();}}
    private static string DisplayName(ApplicationTrafficSnapshot app)=>NonEmpty(app.Description,NonEmpty(app.ProcessName,"Application")); private static string NonEmpty(string? value,string fallback)=>string.IsNullOrWhiteSpace(value)?fallback:value;

    public void Acknowledge(Guid id,string? note){lock(_gate){var e=_history.FirstOrDefault(x=>x.Id==id);if(e is null)return;e.Status=AlertStatus.Acknowledged;e.AcknowledgedUtc=DateTime.UtcNow;e.AcknowledgementNote=note;SaveUnsafe();}}
    public void Resolve(Guid id){lock(_gate){var e=_history.FirstOrDefault(x=>x.Id==id);if(e is null)return;e.Status=AlertStatus.Resolved;e.ResolvedUtc=DateTime.UtcNow;SaveUnsafe();}}
    public void ClearHistory(){lock(_gate){_history.Clear();SaveUnsafe();}}
    public void Save(){lock(_gate)SaveUnsafe();}
    private void Prune(DateTime now){var cutoff=now.AddDays(-_retentionDays);_history.RemoveAll(x=>x.TriggeredUtc<cutoff);if(_history.Count>_maximumEvents)_history.RemoveRange(0,_history.Count-_maximumEvents);}
    private void LoadHistory(){try{if(!File.Exists(_historyPath))return;var items=JsonSerializer.Deserialize<List<AlertEvent>>(File.ReadAllText(_historyPath));if(items is not null)_history.AddRange(items);Prune(DateTime.UtcNow);}catch{}}
    private void SaveUnsafe(){var temp=_historyPath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(_history,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,_historyPath,true);}
    public void Dispose()=>Save();
}
