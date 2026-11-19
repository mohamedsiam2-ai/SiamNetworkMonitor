namespace SiamNetworkMonitor.Models;

public sealed class ProcessTrafficInfo
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = "";
    public double DownloadKBps { get; init; }
    public double UploadKBps { get; init; }
    public double TotalKBps => DownloadKBps + UploadKBps;
}
