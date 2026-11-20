using System.IO;using System.IO.Compression;using System.Text;using System.Text.Json;
namespace SiamNetworkMonitor.Services;
public sealed class DiagnosticsExportService
{
 public void Export(string zipPath,string diagnostics)
 {
  string temp=Path.Combine(Path.GetTempPath(),"SiamNetworkMonitor-Diagnostics-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try
  {
   string user=Environment.UserName;string profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
   string clean=diagnostics.Replace(user,"<USER>",StringComparison.OrdinalIgnoreCase).Replace(profile,"<USERPROFILE>",StringComparison.OrdinalIgnoreCase);
   File.WriteAllText(Path.Combine(temp,"diagnostics.txt"),clean,Encoding.UTF8);
   var local=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SiamNetworkMonitor");
   var files=new[]{"usage-history.json","alert-rules.json","alert-history.json","app-settings.json","data-limit-settings.json","v18-ui-settings.json"};
   var health=new StringBuilder();foreach(string name in files){string p=Path.Combine(local,name);if(!File.Exists(p)){health.AppendLine($"{name}: Not created");continue;}try{using var _=JsonDocument.Parse(File.ReadAllText(p));health.AppendLine($"{name}: Valid JSON ({new FileInfo(p).Length} bytes)");}catch(Exception ex){health.AppendLine($"{name}: INVALID JSON - {ex.Message}");}}
   File.WriteAllText(Path.Combine(temp,"file-health.txt"),health.ToString(),Encoding.UTF8);
   var rulesPath=Path.Combine(local,"alert-rules.json");var ruleReport=new StringBuilder("Alert rule validation\n");if(File.Exists(rulesPath)){try{using var json=JsonDocument.Parse(File.ReadAllText(rulesPath));int total=json.RootElement.ValueKind==JsonValueKind.Array?json.RootElement.GetArrayLength():0;int enabled=json.RootElement.ValueKind==JsonValueKind.Array?json.RootElement.EnumerateArray().Count(x=>x.TryGetProperty("Enabled",out var e)&&e.GetBoolean()):0;ruleReport.AppendLine($"Total rules: {total}\nEnabled rules: {enabled}");}catch(Exception ex){ruleReport.AppendLine("Unable to validate: "+ex.Message);}}else ruleReport.AppendLine("No alert-rules.json file.");File.WriteAllText(Path.Combine(temp,"alert-rule-validation.txt"),ruleReport.ToString(),Encoding.UTF8);
   if(File.Exists(zipPath))File.Delete(zipPath);ZipFile.CreateFromDirectory(temp,zipPath,CompressionLevel.Optimal,false);
  }
  finally{try{Directory.Delete(temp,true);}catch{}}
 }
}
