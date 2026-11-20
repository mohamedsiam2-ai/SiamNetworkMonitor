using System.IO;using System.Text.Json;using Microsoft.Win32;
namespace SiamNetworkMonitor.Services;
public sealed class AppSettings{public bool StartWithWindows{get;set;}public bool StartMinimized{get;set;}=true;public bool NotificationsEnabled{get;set;}=false;public bool HideMeterInFullscreen{get;set;}=true;public int HistoryRetentionDays{get;set;}=90;}
public sealed class AppSettingsService
{
 readonly string _path;const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";const string ValueName="SiamNetworkMonitor";
 public AppSettingsService(){var f=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SiamNetworkMonitor");Directory.CreateDirectory(f);_path=Path.Combine(f,"app-settings.json");}
 public AppSettings Load(){try{if(File.Exists(_path))return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path))??new();}catch{}return new();}
 public void Save(AppSettings s){var tmp=_path+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(s,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,_path,true);ApplyStartup(s.StartWithWindows);}
 public bool IsStartupEnabled(){using var k=Registry.CurrentUser.OpenSubKey(RunKey);return k?.GetValue(ValueName) is string;}
 void ApplyStartup(bool enabled){using var k=Registry.CurrentUser.CreateSubKey(RunKey);if(enabled){var exe=Environment.ProcessPath??Path.Combine(AppContext.BaseDirectory,"SiamNetworkMonitor.exe");k.SetValue(ValueName,$"\"{exe}\" --startup");}else k.DeleteValue(ValueName,false);}
}
