
using System.Net.NetworkInformation;

namespace VigilantMeshAgent.Services;

public class SystemInfoService : ISystemInfoService
{
    public SystemInfo GetCurrentSystemInfo()
    {
        var (localIp, mac) = GetPrimaryIpv4AndMac();
        var (isConnected, vpnIp, adapterName) = GetVpnInterfaceInfo();

        return new SystemInfo(localIp, mac, isConnected, vpnIp, adapterName);
    }

    public string GetOrCreateClientId()
    {
        string idPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VigilantMeshAgent", "agent_id.txt");
        try
        {
            if (File.Exists(idPath))
            {
                return File.ReadAllText(idPath).Trim();
            }

            var id = Guid.NewGuid().ToString();
            Directory.CreateDirectory(Path.GetDirectoryName(idPath)!);
            File.WriteAllText(idPath, id);
            return id;
        }
        catch
        {
            
            return Guid.NewGuid().ToString();
        }
    }

    private static (string? ip, string? mac) GetPrimaryIpv4AndMac()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(ni => (
                    ip: ni.GetIPProperties().UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString(),
                    mac: string.Join(":", ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")))
                ))
                .FirstOrDefault(x => x.ip != null);
        }
        catch { return (null, null); }
    }

    private static (bool IsConnected, string? IpAddress, string? AdapterName) GetVpnInterfaceInfo()
    {
        try
        {
            var vpnKeywords = new[] { "tun", "tap", "openvpn", "wireguard", "forti", "check point", "tunnel" };

            var vpnInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                   vpnKeywords.Any(kw => (ni.Description + ni.Name).ToLower().Contains(kw)));

            if (vpnInterface == null)
            {
                return (false, null, null);
            }

            var vpnIp = vpnInterface.GetIPProperties().UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                .Address.ToString();

            return (true, vpnIp, vpnInterface.Name);
        }
        catch
        {
            return (false, null, null);
        }
    }
}
