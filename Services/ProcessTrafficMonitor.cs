using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using SiamNetworkMonitor.Models;

namespace SiamNetworkMonitor.Services;

public sealed class ProcessTrafficMonitor : IDisposable
{
    private readonly ConcurrentDictionary<int, Counters> _current = new();
    private readonly CancellationTokenSource _stop = new();
    private TraceEventSession? _session;
    private Task? _worker;
    private string? _error;

    public string? Error => _error;

    public void Start()
    {
        _worker = Task.Run(() =>
        {
            try
            {
                var name = $"SiamNetworkMonitor-{Environment.ProcessId}";
                using var session = new TraceEventSession(name);
                _session = session;
                session.StopOnDispose = true;
                session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                session.Source.Kernel.TcpIpRecv += data => Add(data.ProcessID, data.size, true);
                session.Source.Kernel.TcpIpSend += data => Add(data.ProcessID, data.size, false);
                session.Source.Kernel.UdpIpRecv += data => Add(data.ProcessID, data.size, true);
                session.Source.Kernel.UdpIpSend += data => Add(data.ProcessID, data.size, false);
                session.Source.Process();
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }, _stop.Token);
    }

    private void Add(int pid, int bytes, bool received)
    {
        if (pid <= 0 || bytes <= 0) return;
        var counter = _current.GetOrAdd(pid, _ => new Counters());
        if (received) Interlocked.Add(ref counter.Received, bytes);
        else Interlocked.Add(ref counter.Sent, bytes);
    }

    public IReadOnlyList<ProcessTrafficInfo> TakeSample(double seconds)
    {
        seconds = Math.Max(seconds, 0.1);
        var rows = new List<ProcessTrafficInfo>();
        foreach (var pair in _current.ToArray())
        {
            if (!_current.TryRemove(pair.Key, out var counter)) continue;
            var received = Interlocked.Read(ref counter.Received);
            var sent = Interlocked.Read(ref counter.Sent);
            if (received == 0 && sent == 0) continue;
            rows.Add(new ProcessTrafficInfo
            {
                ProcessId = pair.Key,
                ProcessName = ResolveName(pair.Key),
                DownloadKBps = received / 1024d / seconds,
                UploadKBps = sent / 1024d / seconds
            });
        }
        return rows.OrderByDescending(x => x.TotalKBps).ToList();
    }


    private static string ResolveName(int pid)
    {
        try { return Process.GetProcessById(pid).ProcessName + ".exe"; }
        catch { return $"PID {pid}"; }
    }

    public void Dispose()
    {
        _stop.Cancel();
        try { _session?.Dispose(); } catch { }
        try { _worker?.Wait(1000); } catch { }
        _stop.Dispose();
    }

    private sealed class Counters
    {
        public long Received;
        public long Sent;
    }
}

