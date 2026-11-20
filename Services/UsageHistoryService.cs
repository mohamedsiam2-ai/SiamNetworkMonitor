using System.IO;
using System.Text.Json;

namespace SiamNetworkMonitor.Services;

public sealed record UsageBucket(DateTime Hour, string Application, double DownloadKB, double UploadKB);

public sealed class UsageHistoryService : IDisposable
{
    private readonly string _folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SiamNetworkMonitor");
    private readonly string _path;
    private readonly Dictionary<string,UsageBucket> _buckets=new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastSave=DateTime.MinValue;
    public UsageHistoryService(){Directory.CreateDirectory(_folder);_path=Path.Combine(_folder,"usage-history.json");Load();Cleanup();}
    private static DateTime Hour(DateTime value)=>new(value.Year,value.Month,value.Day,value.Hour,0,0,DateTimeKind.Local);
    private static string Key(DateTime hour,string app)=>$"{hour:yyyyMMddHH}|{app}";
    public void Add(string app,double downKB,double upKB)
    {
        if(downKB<=0&&upKB<=0)return;var h=Hour(DateTime.Now);var k=Key(h,app);
        if(_buckets.TryGetValue(k,out var b))_buckets[k]=b with{DownloadKB=b.DownloadKB+downKB,UploadKB=b.UploadKB+upKB};else _buckets[k]=new(h,app,downKB,upKB);
        if(DateTime.Now-_lastSave>=TimeSpan.FromMinutes(1))Save();
    }
    public Dictionary<string,(double Down,double Up)> Query(DateTime since)
    {
        return _buckets.Values.Where(x=>x.Hour>=since).GroupBy(x=>x.Application,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>(g.Sum(x=>x.DownloadKB),g.Sum(x=>x.UploadKB)),StringComparer.OrdinalIgnoreCase);
    }
    public (double Down,double Up) QueryTotals(DateTime since,DateTime? until=null)
    {
        var rows=_buckets.Values.Where(x=>x.Hour>=since && (until is null || x.Hour<until.Value));
        return(rows.Sum(x=>x.DownloadKB),rows.Sum(x=>x.UploadKB));
    }
    public IReadOnlyList<UsageBucket> QueryBuckets(DateTime since)=>_buckets.Values.Where(x=>x.Hour>=since).OrderBy(x=>x.Hour).ToList();
    public void ExportCsv(string path,DateTime since)
    {
        using var w=new StreamWriter(path,false,new System.Text.UTF8Encoding(true));w.WriteLine("Application,DownloadKB,UploadKB,TotalKB");
        foreach(var x in Query(since).OrderByDescending(x=>x.Value.Down+x.Value.Up))w.WriteLine($"\"{x.Key.Replace("\"","\"\"")}\",{x.Value.Down:F2},{x.Value.Up:F2},{x.Value.Down+x.Value.Up:F2}");
    }
    private void Load(){try{if(!File.Exists(_path))return;var items=JsonSerializer.Deserialize<List<UsageBucket>>(File.ReadAllText(_path))??new List<UsageBucket>();foreach(UsageBucket b in items)_buckets[Key(b.Hour,b.Application)]=b;}catch{}}
    private void Cleanup(){int days=new AppSettingsService().Load().HistoryRetentionDays;days=Math.Clamp(days,30,365);var cutoff=DateTime.Now.AddDays(-days);foreach(var k in _buckets.Where(x=>x.Value.Hour<cutoff).Select(x=>x.Key).ToList())_buckets.Remove(k);}
    private void Save(){try{Cleanup();var tmp=_path+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(_buckets.Values.OrderBy(x=>x.Hour)));File.Move(tmp,_path,true);_lastSave=DateTime.Now;}catch{}}
    public void Dispose()=>Save();
}
