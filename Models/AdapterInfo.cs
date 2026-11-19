namespace SiamNetworkMonitor.Models;

public sealed class AdapterInfo
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Type { get; init; } = "";
    public string IPv4 { get; init; } = "-";
    public string Gateway { get; init; } = "-";
    public string MacAddress { get; init; } = "-";
    public double LinkSpeedMbps { get; init; }
    public double DownloadKBps { get; init; }
    public double UploadKBps { get; init; }
    public long BytesReceived { get; init; }
    public long BytesSent { get; init; }
}
