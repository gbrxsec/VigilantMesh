using System.Collections.Concurrent;
using VigilantMeshServer.Models;

namespace VigilantMeshServer.Services;

public class ClientStateService
{
    private readonly ConcurrentDictionary<string, bool> _clientVpnState = new();
    private readonly DiscordNotificationService _notifier;

    public ClientStateService(DiscordNotificationService notifier)
    {
        _notifier = notifier;
    }

    public async Task CheckAndNotifyStateChangeAsync(Heartbeat newHeartbeat)
    {
        var clientId = newHeartbeat.ClientId;
        var newVpnStatus = newHeartbeat.VpnConnected;

        _clientVpnState.TryGetValue(clientId, out var oldVpnStatus);

        if (newVpnStatus != oldVpnStatus)
        {
            
            
            await _notifier.SendConnectionNotificationAsync(newHeartbeat, newVpnStatus);

            _clientVpnState[clientId] = newVpnStatus;
        }
    }
}
