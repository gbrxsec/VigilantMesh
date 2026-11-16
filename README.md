# VigilantMesh

VigilantMesh is a Windows agent and central API for receiving authenticated heartbeats, tracking client availability and sending operational notifications. It is designed for infrastructure teams that need to know when a monitored machine stops communicating and which technical environment was associated with the last signal.

## Components

- `VigilantMesh.Agent`: Windows Worker Service that sends signed heartbeats.
- `VigilantMesh.Server`: ASP.NET Core API that validates HMAC signatures, applies rate limiting, stores a bounded in-memory history and sends optional Discord notifications.
- `VigilantMesh.Encryptor`: Windows DPAPI helper for protecting local configuration values.

## Privacy and attribution

The agent can report hostname, Windows username, local IP, MAC address and VPN adapter metadata. These fields can identify people or organizations. Use this project only with authorization, document the purpose and retention period, and configure the minimum metadata required.

The server may correlate a client ID, VPN address or adapter with an environment label. This is evidence correlation, not proof that a person caused an outage. Notifications should say which signal was last received and when, rather than assigning blame automatically.

## Quick start

1. Install .NET 8 on the Windows agent and server.
2. Set `Heartbeat:ServerUrl` and `Heartbeat:ApiKey` on the agent.
3. Set the same secret in `Security:ApiKey` on the server.
4. Optionally run `VigilantMesh.Encryptor` on Windows to create `appsettings.protected.json`; the Agent loads this file when present.
5. Use a valid HTTPS endpoint in production. The public example intentionally contains no secret, webhook or VPN mapping.
6. Configure notification URLs through environment variables or a secret manager, not Git.

PowerShell examples:

```powershell
$env:Heartbeat__ServerUrl = "https://monitor.example/heartbeat"
$env:Heartbeat__ApiKey = "replace-with-a-long-random-secret"
$env:Security__ApiKey = "replace-with-a-long-random-secret"
```

The agent signs `{unixTimestamp}.{requestBody}` with HMAC-SHA256 and sends the result in `X-Request-Timestamp` and `X-Signature`. The server rejects missing, stale or invalid signatures.

## Development

```powershell
dotnet restore .\src\VigilantMesh.Agent\VigilantMesh.Agent.csproj
dotnet restore .\src\VigilantMesh.Server\VigilantMesh.Server.csproj
dotnet run --project .\src\VigilantMesh.Server\VigilantMesh.Server.csproj
dotnet run --project .\src\VigilantMesh.Agent\VigilantMesh.Agent.csproj
```

Before production, add persistent storage, TLS termination, secret rotation, structured audit retention and tests for replay prevention, malformed payloads, network timeouts and notification failures.
