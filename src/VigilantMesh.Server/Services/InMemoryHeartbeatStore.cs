using VigilantMeshServer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace VigilantMeshServer.Services
{
    public class InMemoryHeartbeatStore : IHeartbeatStore, IDisposable
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Heartbeat>> _store = new();
        private readonly TimeSpan _retention;
        private readonly int _maxPerClient;

        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public InMemoryHeartbeatStore(TimeSpan retention, int maxPerClient = 500)
        {
            _retention = retention;
            _maxPerClient = maxPerClient;

            
            _cleanupTimer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public void Add(Heartbeat heartbeat)
        {
            var q = _store.GetOrAdd(heartbeat.ClientId ?? "unknown", _ => new ConcurrentQueue<Heartbeat>());
            q.Enqueue(heartbeat);

            
            while (q.Count > _maxPerClient)
            {
                q.TryDequeue(out _);
            }
        }

        public IEnumerable<Heartbeat> GetRecent(TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;
            return _store.Values.SelectMany(q => q.Where(h => h.Timestamp >= cutoff)).ToArray();
        }

        public IEnumerable<Heartbeat> GetByClient(string clientId)
        {
            if (_store.TryGetValue(clientId, out var q))
                return q.ToArray();

            return Array.Empty<Heartbeat>();
        }

        public IEnumerable<string> GetClients()
        {
            return _store.Keys.ToArray();
        }

        
        public (int ActiveClients, int TotalHeartbeats, DateTime? Oldest, DateTime? Newest) GetStats(TimeSpan window)
        {
            var cutoff = DateTime.UtcNow - window;

            var recentHeartbeats = _store.Values.SelectMany(q => q.Where(h => h.Timestamp >= cutoff)).ToArray();
            var activeClients = recentHeartbeats.Select(h => h.ClientId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var total = recentHeartbeats.Length;
            var oldest = recentHeartbeats.Length > 0 ? recentHeartbeats.Min(h => h.Timestamp) : (DateTime?)null;
            var newest = recentHeartbeats.Length > 0 ? recentHeartbeats.Max(h => h.Timestamp) : (DateTime?)null;

            return (activeClients, total, oldest, newest); 
        }

        
        private void Cleanup()
        {
            try
            {
                var cutoff = DateTime.UtcNow - _retention;
                foreach (var kv in _store)
                {
                    var q = kv.Value;
                    var anyRecent = q.Any(h => h.Timestamp >= cutoff);
                    if (!anyRecent)
                    {
                        _store.TryRemove(kv.Key, out _);
                    }
                }
            }
            catch
            {
                
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cleanupTimer?.Dispose();
        }
    }
}
