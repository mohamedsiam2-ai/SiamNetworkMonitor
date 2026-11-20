using System.IO;using System.Text.Json;
namespace SiamNetworkMonitor.Services;
public sealed class SharedMeterSettings{public double Left{get;set;}public double Top{get;set;}public bool Locked{get;set;}public string? Layout{get;set;}="Wide";public double FontSize{get;set;}=16;public double Width{get;set;}=258;}
public sealed class MeterSettingsService
{
 readonly string _path=Path.Combine(AppContext.BaseDirectory,"meter-settings.json");
 public SharedMeterSettings Load(){try{if(File.Exists(_path))return JsonSerializer.Deserialize<SharedMeterSettings>(File.ReadAllText(_path))??new();}catch{}return new();}
 public void Save(SharedMeterSettings s){s.Width=Math.Clamp(s.Width,190,600);File.WriteAllText(_path,JsonSerializer.Serialize(s));}
 public void Reset(){try{if(File.Exists(_path))File.Delete(_path);}catch{}}
}
