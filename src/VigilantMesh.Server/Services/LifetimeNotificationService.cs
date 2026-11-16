

using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace VigilantMeshServer.Services;


public class LifetimeNotificationService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _webhookUrl;

    public LifetimeNotificationService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        
        
        _webhookUrl = _config["Discord:WebhookUrlAlerts"];
    }

    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_webhookUrl)) return Task.CompletedTask;

        var payload = new
        {
            content = "🟢 **Servidor Iniciado**",
            embeds = new[]
            {
                new
                {
                    description = $"O servidor de monitoramento está online e operacional.\n`{DateTime.UtcNow:O}`",
                    color = 3066993 
                }
            }
        };
        return SendWebhookAsync(payload);
    }

    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_webhookUrl)) return Task.CompletedTask;

        var payload = new
        {
            content = "🔴 **Servidor Desligado**",
            embeds = new[]
            {
                new
                {
                    description = $"O servidor de monitoramento foi desligado de forma limpa.\n`{DateTime.UtcNow:O}`",
                    color = 15158332 
                }
            }
        };
        
        SendWebhookAsync(payload).Wait(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }

    private async Task SendWebhookAsync(object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync(_webhookUrl, content);
        }
        catch
        {
            
        }
    }
}
