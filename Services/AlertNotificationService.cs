using System.Drawing;
using System.Windows.Forms;
using SiamNetworkMonitor.Alerts;
namespace SiamNetworkMonitor.Services;
public sealed class AlertNotificationService : IDisposable
{
    private readonly NotifyIcon _icon = new()
    {
        Visible = false,
        Icon = SystemIcons.Warning,
        Text = "Siam Network Monitor Alerts"
    };
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 10000 };

    public AlertNotificationService()
    {
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _icon.Visible = false;
        };
    }

    public void Show(AlertEvent alert)
    {
        if (!new AppSettingsService().Load().NotificationsEnabled) return;
        _icon.BalloonTipTitle = $"{alert.Severity}: {alert.RuleName}";
        _icon.BalloonTipText = alert.EvidenceSummary.Length > 240
            ? alert.EvidenceSummary[..240]
            : alert.EvidenceSummary;
        _icon.BalloonTipIcon = alert.Severity == AlertSeverity.Critical
            ? ToolTipIcon.Error
            : ToolTipIcon.Warning;
        _icon.Visible = true;
        _icon.ShowBalloonTip(7000);
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    public void Dispose()
    {
        _hideTimer.Stop();
        _hideTimer.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
    }
}
