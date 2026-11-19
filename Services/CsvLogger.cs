using System.Globalization;
using System.IO;
using System.Text;

namespace SiamNetworkMonitor.Services;

public sealed class CsvLogger : IDisposable
{
    private readonly string _logDirectory;
    private StreamWriter? _writer;
    private DateOnly _openDay;
    private int _pendingLines;

    public CsvLogger()
    {
        _logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public void Log(double downloadKBps, double uploadKBps)
    {
        var now = DateTime.Now;
        EnsureWriter(DateOnly.FromDateTime(now));
        _writer!.Write(now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        _writer.Write(',');
        _writer.Write(downloadKBps.ToString("F2", CultureInfo.InvariantCulture));
        _writer.Write(',');
        _writer.WriteLine(uploadKBps.ToString("F2", CultureInfo.InvariantCulture));
        if (++_pendingLines >= 10) { _writer.Flush(); _pendingLines = 0; }
    }

    private void EnsureWriter(DateOnly day)
    {
        if (_writer is not null && day == _openDay) return;
        _writer?.Dispose();
        _openDay = day;
        var path = Path.Combine(_logDirectory, $"usage_{day:yyyy-MM-dd}.csv");
        var isNew = !File.Exists(path) || new FileInfo(path).Length == 0;
        _writer = new StreamWriter(path, append: true, new UTF8Encoding(false), 4096) { AutoFlush = false };
        if (isNew) _writer.WriteLine("Timestamp,DownloadKBps,UploadKBps");
        _pendingLines = 0;
    }

    public void Dispose() { _writer?.Dispose(); _writer = null; }
}
