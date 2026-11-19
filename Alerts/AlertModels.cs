namespace SiamNetworkMonitor.Alerts;

public enum AlertRuleType { ApplicationSessionUsage, DailyTotalUsage, SustainedDownloadSpeed, SustainedUploadSpeed, UnsignedApplicationUsage, UploadHeavyActivity }
public enum AlertScope { AllApplications, SpecificApplication }
public enum AlertSeverity { Information, Warning, Critical }
public enum AlertStatus { Active, Acknowledged, Resolved }
public enum NotificationMethod { None, WindowsNotification, WindowsNotificationAndSound }

public sealed class AlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid(); public string Name { get; set; } = ""; public bool Enabled { get; set; } = true;
    public AlertRuleType Type { get; set; } public AlertScope Scope { get; set; } = AlertScope.AllApplications;
    public string? ProcessName { get; set; } public string? ExecutablePath { get; set; }
    public double ThresholdKB { get; set; } public double ThresholdKBps { get; set; } public int SustainedSeconds { get; set; }
    public double MinimumTrafficKB { get; set; } public int CooldownMinutes { get; set; } = 15;
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning; public NotificationMethod Notification { get; set; } = NotificationMethod.WindowsNotification;
    public bool RequireUnsignedApplication { get; set; } public bool IncludeUnknownSignature { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record ApplicationTrafficSnapshot
{
    public DateTime TimestampUtc { get; init; } public int ProcessId { get; init; } public string ProcessName { get; init; } = "";
    public string Description { get; init; } = ""; public string Publisher { get; init; } = ""; public string ExecutablePath { get; init; } = "";
    public bool? IsSigned { get; init; } public double DownloadKBps { get; init; } public double UploadKBps { get; init; }
    public double SessionDownloadKB { get; init; } public double SessionUploadKB { get; init; }
    public double TodayDownloadKB { get; init; } public double TodayUploadKB { get; init; }
    public double SessionTotalKB => SessionDownloadKB + SessionUploadKB; public double TodayTotalKB => TodayDownloadKB + TodayUploadKB;
}

public sealed class AlertEvent
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid RuleId { get; set; } public string RuleName { get; set; } = "";
    public AlertRuleType RuleType { get; set; } public AlertSeverity Severity { get; set; } public AlertStatus Status { get; set; } = AlertStatus.Active;
    public DateTime TriggeredUtc { get; set; } public DateTime? AcknowledgedUtc { get; set; } public DateTime? ResolvedUtc { get; set; }
    public int ProcessId { get; set; } public string ProcessName { get; set; } = ""; public string Description { get; set; } = "";
    public string Publisher { get; set; } = ""; public string ExecutablePath { get; set; } = ""; public bool? IsSigned { get; set; }
    public double ObservedValue { get; set; } public double ThresholdValue { get; set; } public string Unit { get; set; } = "";
    public int SustainedSeconds { get; set; } public double DownloadKBps { get; set; } public double UploadKBps { get; set; }
    public double SessionDownloadKB { get; set; } public double SessionUploadKB { get; set; } public double TodayDownloadKB { get; set; } public double TodayUploadKB { get; set; }
    public string EvidenceSummary { get; set; } = ""; public string? AcknowledgementNote { get; set; }
}

internal sealed class AlertEvaluationState
{
    public DateTime? ThresholdStartedUtc { get; set; } public DateTime? LastTriggeredUtc { get; set; }
    public double PeakObservedValue { get; set; } public bool IsCurrentlyAboveThreshold { get; set; } public bool TriggeredForCurrentCrossing { get; set; }
}

public interface IAlertEvaluator
{
    IReadOnlyList<AlertEvent> Evaluate(IReadOnlyCollection<ApplicationTrafficSnapshot> applications, DateTime timestampUtc);
    void Acknowledge(Guid alertEventId, string? note); void Resolve(Guid alertEventId); void ClearHistory(); void Save();
}
