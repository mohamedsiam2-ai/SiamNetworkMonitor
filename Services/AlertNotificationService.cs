using System.Drawing;
using System.Windows.Forms;
using SiamNetworkMonitor.Alerts;
namespace SiamNetworkMonitor.Services;
public sealed class AlertNotificationService:IDisposable
{
 private readonly NotifyIcon _icon=new(){Visible=true,Icon=SystemIcons.Warning,Text="Siam Network Monitor Alerts"};
 public void Show(AlertEvent e){if(!new AppSettingsService().Load().NotificationsEnabled)return;_icon.BalloonTipTitle=$"{e.Severity}: {e.RuleName}";_icon.BalloonTipText=e.EvidenceSummary.Length>240?e.EvidenceSummary[..240]:e.EvidenceSummary;_icon.BalloonTipIcon=e.Severity==AlertSeverity.Critical?ToolTipIcon.Error:ToolTipIcon.Warning;_icon.ShowBalloonTip(7000);}
 public void Dispose(){_icon.Visible=false;_icon.Dispose();}
}
