
namespace VigilantMeshAgent.Services;

public record SystemInfo(
    string? LocalIp,
    string? MacAddress,
    bool IsVpnConnected,
    string? VpnIpAddress,
    string? VpnAdapterName
 );

public interface ISystemInfoService
{
    SystemInfo GetCurrentSystemInfo();
    string GetOrCreateClientId();
}
