

using VigilantMeshServer.Models;
using System;
using System.Collections.Generic;

namespace VigilantMeshServer.Services
{
    public record HeartbeatStats(int ActiveClients, int TotalHeartbeats, DateTime? Oldest, DateTime? Newest);

    public interface IHeartbeatStore
    {
        void Add(Heartbeat heartbeat);
        IEnumerable<Heartbeat> GetRecent(TimeSpan window);
        IEnumerable<Heartbeat> GetByClient(string clientId);
        IEnumerable<string> GetClients();
        (int ActiveClients, int TotalHeartbeats, DateTime? Oldest, DateTime? Newest) GetStats(TimeSpan window);
    }
}
