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
    private System.Windows.Point _dragStart; private double _startLeft,_startTop,_startWidth; private bool _dragging,_resizing,_resizeFromLeft,_moved,_locked; private string _layout="Wide"; private double _fontSize=16; private IntPtr _lastTaskbar; private bool _hiddenForFullscreen;
    public event Action? OpenRequested; public event Action? ExitRequested;

    public TaskbarMeterWindow()
    {
        InitializeComponent();
        Loaded+=(_,_)=>{LoadSettings();ApplyToolWindowStyle();AttachToTaskbarOwner();};
        MouseLeftButtonDown+=OnDown; MouseMove+=OnMove; MouseLeftButtonUp+=OnUp; MouseLeave+=OnMouseLeave;
        ContextMenu=BuildMenu();
        _attachTimer=new(){Interval=TimeSpan.FromMilliseconds(500)}; _attachTimer.Tick+=(_,_)=>{AttachToTaskbarOwner();UpdateFullscreenVisibility();}; _attachTimer.Start(); Closed+=(_,_)=>_attachTimer.Stop();
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
        m.Items.Add(Item("Reset position and width",()=>{Width=258;PositionNearTaskbar();SaveSettings();})); m.Items.Add(new System.Windows.Controls.Separator()); m.Items.Add(Item("Exit",()=>ExitRequested?.Invoke())); return m;
    }

    public void Update(double down,double up){DownText.Text=DisplayFormatter.Rate(down);UpText.Text=DisplayFormatter.Rate(up);}
    private void SetLayout(string value){_layout=value;bool compact=value=="Compact";Width=compact?190:Math.Max(258,Width);DownLabel.Visibility=UpLabel.Visibility=compact?Visibility.Collapsed:Visibility.Visible;DownArrow.FontSize=UpArrow.FontSize=compact?18:22;SaveSettings();}
    private void SetFont(double value){_fontSize=value;DownText.FontSize=UpText.FontSize=value;Height=Math.Max(48,value+36);SaveSettings();}
    private bool IsResizeEdge(System.Windows.Point point,out bool fromLeft){const double edge=10;fromLeft=point.X<=edge;return fromLeft||point.X>=ActualWidth-edge;}
    private void UpdateResizeCursor(System.Windows.Point point){Cursor=!_locked&&IsResizeEdge(point,out _)?System.Windows.Input.Cursors.SizeWE:System.Windows.Input.Cursors.SizeAll;}
    private void OnDown(object s,MouseButtonEventArgs e)
    {
        if(e.ClickCount==2){OpenRequested?.Invoke();return;}
        if(_locked)return;
        var local=e.GetPosition(this);_dragStart=PointToScreen(local);_startLeft=Left;_startTop=Top;_startWidth=Width;_moved=false;
        _resizing=IsResizeEdge(local,out _resizeFromLeft);_dragging=!_resizing;CaptureMouse();e.Handled=true;
    }
    private void OnMove(object s,System.Windows.Input.MouseEventArgs e)
    {
        var local=e.GetPosition(this);
        if(!_dragging&&!_resizing){UpdateResizeCursor(local);return;}
        if(e.LeftButton!=MouseButtonState.Pressed)return;
        var p=PointToScreen(local);var dx=p.X-_dragStart.X;var dy=p.Y-_dragStart.Y;
        if(Math.Abs(dx)>2||Math.Abs(dy)>2)_moved=true;
        if(_resizing)
        {
            double requested=_resizeFromLeft?_startWidth-dx:_startWidth+dx;
            double next=Math.Clamp(requested,190,600);
            if(_resizeFromLeft)Left=_startLeft+(_startWidth-next);
            Width=next;
        }
        else{Left=_startLeft+dx;Top=_startTop+dy;}
    }
    private void OnUp(object s,MouseButtonEventArgs e)
    {
        if(!_dragging&&!_resizing){if(!_locked)OpenRequested?.Invoke();return;}
        bool wasResize=_resizing;_dragging=false;_resizing=false;ReleaseMouseCapture();Clamp();SaveSettings();UpdateResizeCursor(e.GetPosition(this));
        if(!_moved&&!wasResize)OpenRequested?.Invoke();e.Handled=true;
    }
    private void OnMouseLeave(object s,System.Windows.Input.MouseEventArgs e){if(!_dragging&&!_resizing)Cursor=System.Windows.Input.Cursors.SizeAll;}
    private void PositionNearTaskbar(){var a=SystemParameters.WorkArea;Left=Math.Max(a.Left+8,a.Right-Width-12);Top=Math.Max(a.Top+8,a.Bottom-Height-8);}
    private void Clamp(){var l=SystemParameters.VirtualScreenLeft;var t=SystemParameters.VirtualScreenTop;var r=l+SystemParameters.VirtualScreenWidth;var b=t+SystemParameters.VirtualScreenHeight;const double grip=40;Left=Math.Clamp(Left,l-Width+grip,r-grip);Top=Math.Clamp(Top,t-Height+grip,b-grip);}
    private void SaveSettings(){try{System.IO.File.WriteAllText(_settingsPath,JsonSerializer.Serialize(new MeterSettings(Left,Top,_locked,_layout,_fontSize,Width)));}catch{}}
    private void LoadSettings(){try{if(System.IO.File.Exists(_settingsPath)){var p=JsonSerializer.Deserialize<MeterSettings>(System.IO.File.ReadAllText(_settingsPath));if(p!=null){Left=p.Left;Top=p.Top;_locked=p.Locked;SetLayout(p.Layout??"Wide");SetFont(Math.Clamp(p.FontSize,14,20));Width=Math.Clamp(p.Width<=0?(p.Layout=="Compact"?190:258):p.Width,190,600);Clamp();return;}}}catch{}PositionNearTaskbar();}
    private void UpdateFullscreenVisibility()
    {
        if (!new AppSettingsService().Load().HideMeterInFullscreen)
        {
            RestoreAfterFullscreen();
            return;
        }

        bool shouldHide = IsForegroundWindowFullscreen();
        if (shouldHide && !_hiddenForFullscreen)
        {
            _hiddenForFullscreen = true;
            Hide();
        }
        else if (!shouldHide)
        {
            RestoreAfterFullscreen();
            EnsureVisibleAfterShowDesktop();
        }
    }

    private void RestoreAfterFullscreen()
    {
        if (!_hiddenForFullscreen) return;
        _hiddenForFullscreen = false;
        Show();
        EnsureVisibleAfterShowDesktop();
    }

    private void EnsureVisibleAfterShowDesktop()
    {
        if (_hiddenForFullscreen) return;
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        // Win+D may minimize or hide taskbar-owned tool windows even though the
        // fullscreen detector correctly identifies the desktop. Restore without
        // activation so the desktop remains focused.
        if (WindowState != WindowState.Normal) WindowState = WindowState.Normal;
        if (!IsVisible) Show();
        ShowWindow(hwnd, SwShowNoActivate);
        ApplyToolWindowStyle();
        _lastTaskbar = IntPtr.Zero;
        AttachToTaskbarOwner();
        SetWindowPos(hwnd, HwndTopmost, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
    }

    private bool IsForegroundWindowFullscreen()
    {
        IntPtr foreground = GetForegroundWindow();
        IntPtr meter = new WindowInteropHelper(this).Handle;
        if (foreground == IntPtr.Zero || foreground == meter || IsIconic(foreground)) return false;

        // Win+D / Show Desktop is a shell action, not a full-screen application.
        // Depending on the Windows 11 build, the foreground HWND can be the shell
        // window itself or one of several Explorer desktop host classes.
        if (foreground == GetShellWindow()) return false;

        var className = new System.Text.StringBuilder(256);
        GetClassName(foreground, className, className.Capacity);
        string windowClass = className.ToString();
        if (windowClass is "Shell_TrayWnd"
            or "Shell_SecondaryTrayWnd"
            or "Progman"
            or "WorkerW"
            or "SHELLDLL_DefView"
            or "FolderView"
            or "XamlExplorerHostIslandWindow"
            or "Windows.UI.Core.CoreWindow") return false;

        // Explorer owns the desktop surface used by Show Desktop. Exclude only
        // desktop-sized Explorer windows, while allowing ordinary File Explorer
        // windows to be evaluated normally.
        GetWindowThreadProcessId(foreground, out uint processId);
        if (processId != 0)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                if (process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)
                    && IsDesktopSizedWindow(foreground)) return false;
            }
            catch { }
        }

        if (!GetWindowRect(foreground, out RECT windowRect)) return false;
        IntPtr monitor = MonitorFromWindow(foreground, MonitorDefaultToNearest);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info)) return false;

        const int tolerance = 3;
        return windowRect.Left <= info.rcMonitor.Left + tolerance
            && windowRect.Top <= info.rcMonitor.Top + tolerance
            && windowRect.Right >= info.rcMonitor.Right - tolerance
            && windowRect.Bottom >= info.rcMonitor.Bottom - tolerance;
    }

    private static bool IsDesktopSizedWindow(IntPtr window)
    {
        if (!GetWindowRect(window, out RECT rect)) return false;
        IntPtr monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info)) return false;
        const int tolerance = 3;
        return rect.Left <= info.rcMonitor.Left + tolerance
            && rect.Top <= info.rcMonitor.Top + tolerance
            && rect.Right >= info.rcMonitor.Right - tolerance
            && rect.Bottom >= info.rcMonitor.Bottom - tolerance;
    }

    private void AttachToTaskbarOwner(){var hwnd=new WindowInteropHelper(this).Handle;if(hwnd==IntPtr.Zero)return;var taskbar=FindWindow("Shell_TrayWnd",null);if(taskbar==IntPtr.Zero||taskbar==_lastTaskbar)return;SetWindowLongPtr(hwnd,-8,taskbar);_lastTaskbar=taskbar;SetWindowPos(hwnd,HwndTopmost,0,0,0,0,SwpNoSize|SwpNoMove|SwpNoActivate|SwpShowWindow);}
    private void ApplyToolWindowStyle(){const int idx=-20,tool=0x80,app=0x40000;var h=new WindowInteropHelper(this).Handle;var style=GetWindowLongPtr(h,idx).ToInt64();SetWindowLongPtr(h,idx,new IntPtr((style|tool)&~app));}
    private sealed record MeterSettings(double Left,double Top,bool Locked,string? Layout,double FontSize,double Width=258);
    private const uint MonitorDefaultToNearest = 2;
    private const int SwShowNoActivate = 4;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int command);
    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder className, int maxCount);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern IntPtr FindWindow(string name,string? title);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]private static extern IntPtr GetWindowLongPtr(IntPtr h,int i);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]private static extern IntPtr SetWindowLongPtr(IntPtr h,int i,IntPtr v);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
}
