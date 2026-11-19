using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using SiamNetworkMonitor.Services;

namespace SiamNetworkMonitor;

public partial class TaskbarMeterWindow : Window
{
    private readonly string _settingsPath=System.IO.Path.Combine(AppContext.BaseDirectory,"meter-settings.json");
    private readonly DispatcherTimer _attachTimer;
    private System.Windows.Point _dragStart; private double _startLeft,_startTop; private bool _dragging,_moved,_locked; private string _layout="Wide"; private double _fontSize=16; private IntPtr _lastTaskbar;
    public event Action? OpenRequested; public event Action? ExitRequested;

    public TaskbarMeterWindow()
    {
        InitializeComponent();
        Loaded+=(_,_)=>{LoadSettings();ApplyToolWindowStyle();AttachToTaskbarOwner();};
        MouseLeftButtonDown+=OnDown; MouseMove+=OnMove; MouseLeftButtonUp+=OnUp;
        ContextMenu=BuildMenu();
        _attachTimer=new(){Interval=TimeSpan.FromSeconds(2)}; _attachTimer.Tick+=(_,_)=>AttachToTaskbarOwner(); _attachTimer.Start(); Closed+=(_,_)=>_attachTimer.Stop();
    }

    private System.Windows.Controls.ContextMenu BuildMenu()
    {
        var m=new System.Windows.Controls.ContextMenu { Background = System.Windows.Media.Brushes.White, Foreground = System.Windows.Media.Brushes.Black };
        System.Windows.Controls.TextBlock BlackHeader(string text) => new() { Text=text, Foreground=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(17,17,17)) };
        System.Windows.Controls.MenuItem Item(string text, Action action){var i=new System.Windows.Controls.MenuItem{Header=BlackHeader(text), Foreground=System.Windows.Media.Brushes.Black, Background=System.Windows.Media.Brushes.White};i.Click+=(_,_)=>action();return i;}
        m.Items.Add(Item("Open dashboard",()=>OpenRequested?.Invoke()));
        var layout=new System.Windows.Controls.MenuItem{Header=BlackHeader("Layout"), Foreground=System.Windows.Media.Brushes.Black, Background=System.Windows.Media.Brushes.White}; layout.Items.Add(Item("Wide",()=>SetLayout("Wide"))); layout.Items.Add(Item("Compact",()=>SetLayout("Compact"))); m.Items.Add(layout);
        var size=new System.Windows.Controls.MenuItem{Header=BlackHeader("Font size"), Foreground=System.Windows.Media.Brushes.Black, Background=System.Windows.Media.Brushes.White}; foreach(var n in new[]{14d,16d,18d,20d}){var v=n;size.Items.Add(Item($"{n:0} pt",()=>SetFont(v)));} m.Items.Add(size);
        var lockItem=new System.Windows.Controls.MenuItem{Header=BlackHeader("Lock position"),IsCheckable=true, Foreground=System.Windows.Media.Brushes.Black, Background=System.Windows.Media.Brushes.White}; lockItem.Checked+=(_,_)=>{_locked=true;SaveSettings();}; lockItem.Unchecked+=(_,_)=>{_locked=false;SaveSettings();}; m.Opened+=(_,_)=>lockItem.IsChecked=_locked; m.Items.Add(lockItem);
        m.Items.Add(Item("Reset position",()=>{PositionNearTaskbar();SaveSettings();})); m.Items.Add(new System.Windows.Controls.Separator()); m.Items.Add(Item("Exit",()=>ExitRequested?.Invoke())); return m;
    }

    public void Update(double down,double up){DownText.Text=DisplayFormatter.Rate(down);UpText.Text=DisplayFormatter.Rate(up);}
    private void SetLayout(string value){_layout=value;bool compact=value=="Compact";Width=compact?190:258;DownLabel.Visibility=UpLabel.Visibility=compact?Visibility.Collapsed:Visibility.Visible;DownArrow.FontSize=UpArrow.FontSize=compact?18:22;SaveSettings();}
    private void SetFont(double value){_fontSize=value;DownText.FontSize=UpText.FontSize=value;Height=Math.Max(48,value+36);SaveSettings();}
    private void OnDown(object s,MouseButtonEventArgs e){if(e.ClickCount==2){OpenRequested?.Invoke();return;}if(_locked)return;_dragStart=PointToScreen(e.GetPosition(this));_startLeft=Left;_startTop=Top;_dragging=true;_moved=false;CaptureMouse();e.Handled=true;}
    private void OnMove(object s,System.Windows.Input.MouseEventArgs e){if(!_dragging||e.LeftButton!=MouseButtonState.Pressed)return;var p=PointToScreen(e.GetPosition(this));var dx=p.X-_dragStart.X;var dy=p.Y-_dragStart.Y;if(Math.Abs(dx)>3||Math.Abs(dy)>3)_moved=true;Left=_startLeft+dx;Top=_startTop+dy;}
    private void OnUp(object s,MouseButtonEventArgs e){if(!_dragging){if(!_locked)OpenRequested?.Invoke();return;}_dragging=false;ReleaseMouseCapture();Clamp();SaveSettings();if(!_moved)OpenRequested?.Invoke();e.Handled=true;}
    private void PositionNearTaskbar(){var a=SystemParameters.WorkArea;Left=Math.Max(a.Left+8,a.Right-Width-12);Top=Math.Max(a.Top+8,a.Bottom-Height-8);}
    private void Clamp(){var l=SystemParameters.VirtualScreenLeft;var t=SystemParameters.VirtualScreenTop;var r=l+SystemParameters.VirtualScreenWidth;var b=t+SystemParameters.VirtualScreenHeight;const double grip=40;Left=Math.Clamp(Left,l-Width+grip,r-grip);Top=Math.Clamp(Top,t-Height+grip,b-grip);}
    private void SaveSettings(){try{System.IO.File.WriteAllText(_settingsPath,JsonSerializer.Serialize(new MeterSettings(Left,Top,_locked,_layout,_fontSize)));}catch{}}
    private void LoadSettings(){try{if(System.IO.File.Exists(_settingsPath)){var p=JsonSerializer.Deserialize<MeterSettings>(System.IO.File.ReadAllText(_settingsPath));if(p!=null){Left=p.Left;Top=p.Top;_locked=p.Locked;SetLayout(p.Layout??"Wide");SetFont(Math.Clamp(p.FontSize,14,20));Clamp();return;}}}catch{}PositionNearTaskbar();}
    private void AttachToTaskbarOwner(){var hwnd=new WindowInteropHelper(this).Handle;if(hwnd==IntPtr.Zero)return;var taskbar=FindWindow("Shell_TrayWnd",null);if(taskbar==IntPtr.Zero||taskbar==_lastTaskbar)return;SetWindowLongPtr(hwnd,-8,taskbar);_lastTaskbar=taskbar;SetWindowPos(hwnd,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010|0x0040);}
    private void ApplyToolWindowStyle(){const int idx=-20,tool=0x80,app=0x40000;var h=new WindowInteropHelper(this).Handle;var style=GetWindowLongPtr(h,idx).ToInt64();SetWindowLongPtr(h,idx,new IntPtr((style|tool)&~app));}
    private sealed record MeterSettings(double Left,double Top,bool Locked,string? Layout,double FontSize);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr FindWindow(string name,string? title);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]private static extern IntPtr GetWindowLongPtr(IntPtr h,int i);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]private static extern IntPtr SetWindowLongPtr(IntPtr h,int i,IntPtr v);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
}
