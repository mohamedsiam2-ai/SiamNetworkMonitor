using System.IO;
using System.Text.Json;
namespace SiamNetworkMonitor.Services;
public sealed class V18UiSettings
{
 public int SelectedTab{get;set;} public double Left{get;set;}=double.NaN;public double Top{get;set;}=double.NaN;public double Width{get;set;}=1040;public double Height{get;set;}=700;
 public string ConnectionSearch{get;set;}="";public int RefreshSeconds{get;set;}public int HistoryDays{get;set;}=7;
}
public sealed class V18UiSettingsService
{
 readonly string _path;public V18UiSettingsService(){var f=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SiamNetworkMonitor");Directory.CreateDirectory(f);_path=Path.Combine(f,"v18-ui-settings.json");}
 public V18UiSettings Load(){try{if(File.Exists(_path))return JsonSerializer.Deserialize<V18UiSettings>(File.ReadAllText(_path))??new();}catch{}return new();}
 public void Save(V18UiSettings v){v.Width=Math.Clamp(v.Width,900,2560);v.Height=Math.Clamp(v.Height,600,1440);var tmp=_path+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(v,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,_path,true);}
}
