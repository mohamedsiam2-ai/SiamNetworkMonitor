using System.Diagnostics;using System.IO;using System.Text;
namespace SiamNetworkMonitor.Services;
public sealed class DiagnosticsService
{
 public string BuildReport()
 {
  var folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SiamNetworkMonitor");var p=Process.GetCurrentProcess();var b=new StringBuilder();
  b.AppendLine("Siam Network Monitor Diagnostics");b.AppendLine($"Generated: {DateTime.Now:O}");b.AppendLine($"Version: {typeof(DiagnosticsService).Assembly.GetName().Version}");b.AppendLine($"OS: {Environment.OSVersion}");b.AppendLine($"64-bit process: {Environment.Is64BitProcess}");b.AppendLine($"Working set: {p.WorkingSet64/1024d/1024d:F1} MB");b.AppendLine($"Threads: {p.Threads.Count}");b.AppendLine($"Elevated: {new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}");
  foreach(var f in new[]{"usage-history.json","alert-rules.json","alert-history.json","app-settings.json","data-limit-settings.json"}){var path=Path.Combine(folder,f);b.AppendLine($"{f}: {(File.Exists(path)?$"OK ({new FileInfo(path).Length} bytes)":"Not created")}");}
  return b.ToString();
 }
}
