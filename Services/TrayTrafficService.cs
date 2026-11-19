using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SiamNetworkMonitor.Services;

public sealed class TrayTrafficService : IDisposable
{
    private readonly NotifyIcon _icon;
    private Icon? _dynamicIcon;
    private IntPtr _dynamicHandle;
    private string _lastIconKey = "";
    public event Action? ShowRequested;
    public event Action? ExitRequested;

    public TrayTrafficService()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open dashboard", null, (_, _) => ShowRequested?.Invoke());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());

        _icon = new NotifyIcon
        {
            Visible = true,
            Text = "Siam Network Monitor",
            ContextMenuStrip = menu
        };
        _icon.DoubleClick += (_, _) => ShowRequested?.Invoke();
        Update(0, 0);
    }

    public void Update(double downKBps, double upKBps)
    {
        var key = $"{Compact(downKBps)}|{Compact(upKBps)}";
        if (key == _lastIconKey) return;
        _lastIconKey = key;
        var (next, handle) = CreateIcon(downKBps, upKBps);
        var oldIcon = _dynamicIcon;
        var oldHandle = _dynamicHandle;

        _dynamicIcon = next;
        _dynamicHandle = handle;
        _icon.Icon = next;


        oldIcon?.Dispose();
        if (oldHandle != IntPtr.Zero) DestroyIcon(oldHandle);
    }

    private static (Icon Icon, IntPtr Handle) CreateIcon(double downKBps, double upKBps)
    {
        // Render at 64x64 so Windows has a sharper source when scaling the tray icon.
        // Windows still controls the final notification-area slot size.
        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var background = new SolidBrush(Color.FromArgb(18, 30, 49));
        using var downBrush = new SolidBrush(Color.FromArgb(45, 140, 255));
        using var upBrush = new SolidBrush(Color.FromArgb(32, 201, 151));
        g.FillRectangle(background, 0, 0, 64, 64);
        g.FillRectangle(downBrush, 0, 0, 64, 43);
        g.FillRectangle(upBrush, 0, 43, 64, 21);

        var downText = Compact(downKBps);
        var upText = Compact(upKBps);
        using var downFont = new Font("Segoe UI", downText.Length > 4 ? 14f : 17f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var upFont = new Font("Segoe UI", upText.Length > 4 ? 10f : 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        DrawCentered(g, downText, downFont, Brushes.White, new RectangleF(0, 2, 64, 40));
        DrawCentered(g, upText, upFont, Brushes.White, new RectangleF(0, 43, 64, 19));

        var sourceHandle = bitmap.GetHicon();
        var cloned = (Icon)Icon.FromHandle(sourceHandle).Clone();
        return (cloned, sourceHandle);
    }

    private static void DrawCentered(Graphics g, string text, Font font, Brush brush, RectangleF area)
    {
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush,
            area.X + (area.Width - size.Width) / 2,
            area.Y + (area.Height - size.Height) / 2);
    }

    private static string Compact(double kbps)
    {
        if (kbps >= 1024 * 1024) return $"{kbps / 1024 / 1024:0.#}G";
        if (kbps >= 1024) return $"{kbps / 1024:0.#}M";
        if (kbps >= 1) return $"{kbps:0}K";
        return "0K";
    }

    private static string Format(double kbps) => kbps >= 1024
        ? $"{kbps / 1024:0.00} MB/s"
        : $"{kbps:0.00} KB/s";

    private static string Limit(string text, int length) =>
        text.Length <= length ? text : text[..length];

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _dynamicIcon?.Dispose();
        if (_dynamicHandle != IntPtr.Zero) DestroyIcon(_dynamicHandle);
    }
}

