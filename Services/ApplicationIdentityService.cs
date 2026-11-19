using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SiamNetworkMonitor.Services;

public sealed record ApplicationIdentity(string Path,string Publisher,string Description,bool Signed);

public sealed class ApplicationIdentityService
{
    private readonly ConcurrentDictionary<string,ApplicationIdentity> _cache=new(StringComparer.OrdinalIgnoreCase);
    public ApplicationIdentity Resolve(int pid,string processName)
    {
        string path="";try{using var p=Process.GetProcessById(pid);path=p.MainModule?.FileName??"";}catch{}
        if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))return new("","Unavailable",processName,false);
        return _cache.GetOrAdd(path,Create);
    }
    private static ApplicationIdentity Create(string path)
    {
        string publisher="Unsigned",description=Path.GetFileNameWithoutExtension(path);bool signed=false;
        try{var info=FileVersionInfo.GetVersionInfo(path);description=string.IsNullOrWhiteSpace(info.FileDescription)?description:info.FileDescription;publisher=string.IsNullOrWhiteSpace(info.CompanyName)?publisher:info.CompanyName;}
        catch{}
        try{using var cert=new X509Certificate2(X509Certificate.CreateFromSignedFile(path));signed=true;if(publisher=="Unsigned")publisher=cert.GetNameInfo(X509NameType.SimpleName,false);}
        catch{}
        return new(path,publisher,description,signed);
    }
}
