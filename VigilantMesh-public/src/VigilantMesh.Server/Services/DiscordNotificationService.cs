

using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VigilantMeshServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace VigilantMeshServer.Services;

public class VpnRule
{
    public string? IpPrefix { get; set; }
    public string? ClientId { get; set; }
    public string Name { get; set; } = "";
}

public class DiscordNotificationService
{
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly IConfiguration _config;
    private readonly bool _isEnabled;
    private readonly string? _connectionsWebhookUrl;

    public DiscordNotificationService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<DiscordNotificationService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _isEnabled = config.GetValue<bool>("Discord:Enabled");
        _connectionsWebhookUrl = config["Discord:WebhookUrlConnections"];

        if (!_isEnabled) _logger.LogWarning("Serviço de notificação do Discord está DESABILITADO.");
        else if (string.IsNullOrEmpty(_connectionsWebhookUrl)) _logger.LogError("A URL do webhook do Discord é INVÁLIDA.");
        else _logger.LogInformation("Serviço de notificação do Discord está HABILITADO.");
    }

    public async Task SendConnectionNotificationAsync(Heartbeat hb, bool isConnected)
    {
        if (!_isEnabled || string.IsNullOrEmpty(_connectionsWebhookUrl)) return;

        _logger.LogInformation("Preparando notificação para {MachineName} (ID: {ClientId})", hb.Hostname, hb.ClientId);

        string vpnDisplayName = "VPN Desconhecida";
        if (isConnected)
        {
            var rules = _config.GetSection("VpnRules").Get<List<VpnRule>>() ?? new List<VpnRule>();
            string? foundName = null;

            
            var clientRule = rules.FirstOrDefault(r =>
                !string.IsNullOrEmpty(r.ClientId) &&
                hb.ClientId.Equals(r.ClientId, StringComparison.OrdinalIgnoreCase)
            );

            if (clientRule != null)
            {
                foundName = clientRule.Name;
                _logger.LogInformation("Regra de ClientId encontrada para {ClientId}: {VpnName}", hb.ClientId, foundName);
            }
            
            else if (!string.IsNullOrEmpty(hb.VpnIp))
            {
                var ipRule = rules.FirstOrDefault(r =>
                    !string.IsNullOrEmpty(r.IpPrefix) &&
                    hb.VpnIp.StartsWith(r.IpPrefix)
                );

                if (ipRule != null)
                {
                    foundName = ipRule.Name;
                    _logger.LogInformation("Regra de IP encontrada para {Ip}: {VpnName}", hb.VpnIp, foundName);
                }
            }

            
            vpnDisplayName = foundName ?? $"Rede não identificada ({hb.VpnAdapterName})";
        }

        
        var emoji = isConnected ? "✅" : "❌";
        var statusText = isConnected ? "Conectou-se à VPN" : "Desconectou-se da VPN";
        var color = isConnected ? 3066993 : 15158332;

        var fields = new List<object>
        {
            new { name = "Usuário Windows", value = hb.Username, inline = true }, 
            new { name = "IP Local", value = hb.LocalIp, inline = true }
        };

        if (isConnected)
        {
            fields.Insert(0, new { name = "Rede VPN", value = $"**{vpnDisplayName}**", inline = false });
            fields.Add(new { name = "IP da VPN", value = hb.VpnIp, inline = true });
        }

        fields.Add(new { name = "Endereço MAC", value = $"`{hb.Mac}`", inline = false });

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    author = new { name = statusText, icon_url = isConnected ? "https://i.imgur.com/4tE3cKj.png" : "https://i.imgur.com/JjF21aG.png" },
                    title = hb.Hostname,
                    color = color,
                    fields = fields,
                    footer = new { text = $"Client ID: {hb.ClientId}" },
                    timestamp = DateTime.UtcNow
                }
            }
        };

        await SendWebhookAsync(_connectionsWebhookUrl, payload);
    }

    private async Task SendWebhookAsync(string url, object payload)
    {
        
        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Falha ao enviar notificação. Status: {StatusCode}. Resposta: {ResponseBody}", response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Erro CRÍTICO ao enviar webhook.");
        }
    }
}
