using System.Diagnostics;
namespace SiamNetworkMonitor.Services;
public sealed record ConnectionRow(string Protocol,string LocalEndpoint,string RemoteEndpoint,string State,int ProcessId,string ProcessName,string Description,string Publisher,string ExecutablePath)
{
 public string PidText=>ProcessId>0?ProcessId.ToString():"-";
}
public sealed class ConnectionInspectorService
{
 readonly ApplicationIdentityService _identity=new();
 public IReadOnlyList<ConnectionRow> GetConnections()
 {
  var rows=new List<ConnectionRow>();var start=new ProcessStartInfo("netstat.exe","-ano"){UseShellExecute=false,RedirectStandardOutput=true,CreateNoWindow=true};using var process=Process.Start(start)??throw new InvalidOperationException("Could not start netstat.exe");string output=process.StandardOutput.ReadToEnd();process.WaitForExit(5000);
  foreach(string raw in output.Split('\n'))
  {
   var parts=raw.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(parts.Length<4)continue;string protocol=parts[0].ToUpperInvariant();if(protocol!="TCP"&&protocol!="UDP")continue;
   string local=parts[1],remote=parts[2],state=protocol=="TCP"?parts[3]:"Listening";string pidText=protocol=="TCP"&&parts.Length>=5?parts[4]:parts[^1];if(!int.TryParse(pidText,out int pid))pid=0;
   string name="Unavailable",description="Unavailable",publisher="Unavailable",path="";if(pid>0){try{using var p=Process.GetProcessById(pid);name=p.ProcessName+".exe";var id=_identity.Resolve(pid,name);description=id.Description;publisher=id.Publisher;path=id.Path;}catch{}}
   rows.Add(new(protocol,local,remote,state,pid,name,description,publisher,path));if(rows.Count>=1000)break;
  }
  return rows.OrderBy(x=>x.ProcessName).ThenBy(x=>x.Protocol).ThenBy(x=>x.LocalEndpoint).ToList();
 }
}
