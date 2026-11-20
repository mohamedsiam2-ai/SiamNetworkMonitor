using System.IO;
using System.Text.Json;

namespace SiamNetworkMonitor.Services;

public sealed class DataLimitSettings
{
    public bool Enabled { get; set; }
    public double AllowanceGB { get; set; } = 500;
    public int CycleStartDay { get; set; } = 1;
    public bool NotificationsEnabled { get; set; }
    public List<int> NotificationThresholds { get; set; } = new() { 50, 75, 90, 100 };
    public string LastNotifiedCycle { get; set; } = "";
    public int HighestNotifiedThreshold { get; set; }
}

public sealed record DataLimitSnapshot(DateTime CycleStart, DateTime CycleEnd, double DownloadKB, double UploadKB, double AllowanceKB)
{
    public double TotalKB => DownloadKB + UploadKB;
    public double RemainingKB => Math.Max(0, AllowanceKB - TotalKB);
    public double Percent => AllowanceKB <= 0 ? 0 : Math.Clamp(TotalKB / AllowanceKB * 100, 0, 999);
    public int ElapsedDays => Math.Max(1, (DateTime.Now.Date - CycleStart.Date).Days + 1);
    public int TotalDays => Math.Max(1, (CycleEnd.Date - CycleStart.Date).Days);
    public double ForecastKB => ElapsedDays < 1 ? TotalKB : TotalKB / ElapsedDays * TotalDays;
}

public sealed class DataLimitService
{
    private readonly string _path;
    public DataLimitService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SiamNetworkMonitor");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "data-limit-settings.json");
    }
    public DataLimitSettings Load(){try{if(File.Exists(_path))return JsonSerializer.Deserialize<DataLimitSettings>(File.ReadAllText(_path))??new();}catch{}return new();}
    public void Save(DataLimitSettings value){value.AllowanceGB=Math.Clamp(value.AllowanceGB,1,100000);value.CycleStartDay=Math.Clamp(value.CycleStartDay,1,28);var tmp=_path+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,_path,true);}
    public static (DateTime Start,DateTime End) GetCycle(int day)
    {
        day=Math.Clamp(day,1,28);var now=DateTime.Now;var thisMonth=new DateTime(now.Year,now.Month,day);
        var start=now>=thisMonth?thisMonth:thisMonth.AddMonths(-1);return(start,start.AddMonths(1));
    }
}
