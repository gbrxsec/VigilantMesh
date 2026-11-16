namespace VigilantMeshAgent.Configuration;

public class HeartbeatSettings
{
    public const string SectionName = "Heartbeat";
    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 15;
}
