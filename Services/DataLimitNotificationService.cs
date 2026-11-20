using System.Drawing;
using System.Windows.Forms;
namespace SiamNetworkMonitor.Services;
public sealed class DataLimitNotificationService:IDisposable
{
 readonly NotifyIcon _icon=new(){Visible=false,Icon=SystemIcons.Information,Text="Siam Network Monitor Data Limit"};readonly System.Windows.Forms.Timer _hide=new(){Interval=10000};
 public DataLimitNotificationService(){_hide.Tick+=(_,_)=>{_hide.Stop();_icon.Visible=false;};}
 public void Show(string title,string message){_icon.BalloonTipTitle=title;_icon.BalloonTipText=message.Length>240?message[..240]:message;_icon.BalloonTipIcon=ToolTipIcon.Info;_icon.Visible=true;_icon.ShowBalloonTip(7000);_hide.Stop();_hide.Start();}
 public void Dispose(){_hide.Stop();_hide.Dispose();_icon.Visible=false;_icon.Dispose();}
}
