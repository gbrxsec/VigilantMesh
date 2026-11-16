using System;
using System.Text.Json.Serialization;

namespace VigilantMeshServer.Models
{
    public record Heartbeat(
        string ClientId,
        string Hostname,
        string Username,
        string LocalIp,
        string Mac,
        DateTime Timestamp,
        bool VpnConnected,
        string VpnIp,           
        string VpnAdapterName   
        );
}
