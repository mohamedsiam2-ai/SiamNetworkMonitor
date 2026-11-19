using System.Net.NetworkInformation;
using System.Net.Sockets;
using SiamNetworkMonitor.Models;

namespace SiamNetworkMonitor.Services;

public sealed class NetworkMonitor
{
    private readonly Dictionary<string, Sample> _previous = new();
    private DateTime _lastSampleTime = DateTime.UtcNow;

    public List<AdapterInfo> GetAdapters()
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = Math.Max(
            (now - _lastSampleTime).TotalSeconds,
            0.001);

        _lastSampleTime = now;
        var result = new List<AdapterInfo>();

        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            try
            {
                var properties = adapter.GetIPProperties();
                var statistics = adapter.GetIPv4Statistics();

                var ipv4 = properties.UnicastAddresses
                    .FirstOrDefault(x =>
                        x.Address.AddressFamily ==
                        AddressFamily.InterNetwork)
                    ?.Address.ToString() ?? "-";

                var gateway = properties.GatewayAddresses
                    .FirstOrDefault(x =>
                        x.Address.AddressFamily ==
                        AddressFamily.InterNetwork)
                    ?.Address.ToString() ?? "-";

                var received = statistics.BytesReceived;
                var sent = statistics.BytesSent;

                double downloadKBps = 0;
                double uploadKBps = 0;

                if (_previous.TryGetValue(adapter.Id, out var previous))
                {
                    downloadKBps = Math.Max(
                        0,
                        (received - previous.Received) /
                        1024d /
                        elapsedSeconds);

                    uploadKBps = Math.Max(
                        0,
                        (sent - previous.Sent) /
                        1024d /
                        elapsedSeconds);
                }

                _previous[adapter.Id] =
                    new Sample(received, sent);

                result.Add(new AdapterInfo
                {
                    Name = adapter.Name,
                    Description = adapter.Description,
                    Type = adapter.NetworkInterfaceType.ToString(),
                    IPv4 = ipv4,
                    Gateway = gateway,
                    MacAddress = FormatMac(
                        adapter.GetPhysicalAddress()),
                    LinkSpeedMbps =
                        Math.Max(0, adapter.Speed / 1_000_000d),
                    DownloadKBps = downloadKBps,
                    UploadKBps = uploadKBps,
                    BytesReceived = received,
                    BytesSent = sent
                });
            }
            catch
            {
                // Adapter may disappear during enumeration.
            }
        }

        return result
            .OrderByDescending(x =>
                x.DownloadKBps + x.UploadKBps)
            .ToList();
    }

    private static string FormatMac(PhysicalAddress address)
    {
        var bytes = address.GetAddressBytes();

        return bytes.Length == 0
            ? "-"
            : string.Join(
                "-",
                bytes.Select(x => x.ToString("X2")));
    }

    private readonly record struct Sample(
        long Received,
        long Sent);
}
