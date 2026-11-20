using SiamNetworkMonitor.Alerts;
namespace SiamNetworkMonitor.Services;
public static class DefaultAlertRules
{
 public static List<AlertRule> Create()=>
 [
  new(){Enabled=false,Name="High application session usage",Type=AlertRuleType.ApplicationSessionUsage,ThresholdKB=500*1024,MinimumTrafficKB=1024,CooldownMinutes=60,Severity=AlertSeverity.Warning},
  new(){Enabled=false,Name="Daily usage warning",Type=AlertRuleType.DailyTotalUsage,ThresholdKB=5*1024*1024,MinimumTrafficKB=1024,CooldownMinutes=720,Severity=AlertSeverity.Warning},
  new(){Enabled=false,Name="Sustained high download",Type=AlertRuleType.SustainedDownloadSpeed,ThresholdKBps=10*1024,SustainedSeconds=10,MinimumTrafficKB=10*1024,CooldownMinutes=15,Severity=AlertSeverity.Information},
  new(){Enabled=false,Name="Sustained high upload",Type=AlertRuleType.SustainedUploadSpeed,ThresholdKBps=2*1024,SustainedSeconds=30,MinimumTrafficKB=25*1024,CooldownMinutes=15,Severity=AlertSeverity.Warning},
  new(){Enabled=false,Name="Unsigned application traffic",Type=AlertRuleType.UnsignedApplicationUsage,ThresholdKB=25*1024,MinimumTrafficKB=25*1024,IncludeUnknownSignature=false,CooldownMinutes=1440,Severity=AlertSeverity.Warning}
 ];
}
