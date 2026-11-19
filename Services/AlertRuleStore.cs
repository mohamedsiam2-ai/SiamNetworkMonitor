using System.IO;
using System.Text.Json;
using SiamNetworkMonitor.Alerts;
namespace SiamNetworkMonitor.Services;
public sealed class AlertRuleStore
{
 readonly string _path;public AlertRuleStore(){var f=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SiamNetworkMonitor");Directory.CreateDirectory(f);_path=Path.Combine(f,"alert-rules.json");}
 public List<AlertRule> Load(){try{if(File.Exists(_path))return JsonSerializer.Deserialize<List<AlertRule>>(File.ReadAllText(_path))??DefaultAlertRules.Create();}catch{}var d=DefaultAlertRules.Create();Save(d);return d;}
 public void Save(IEnumerable<AlertRule> rules){var tmp=_path+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(rules,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,_path,true);}
}
