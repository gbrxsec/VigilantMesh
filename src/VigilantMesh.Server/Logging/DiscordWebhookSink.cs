

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace VigilantMeshServer.Logging;

public class DiscordWebhookSink : ILogEventSink
{
    private readonly string _webhookUrl;
    private readonly IFormatProvider? _formatProvider;
    private static readonly HttpClient _httpClient = new();

    public DiscordWebhookSink(string webhookUrl, IFormatProvider? formatProvider)
    {
        _webhookUrl = webhookUrl;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        
        if (logEvent.Level < LogEventLevel.Error)
        {
            return;
        }

        
        if (logEvent.MessageTemplate.Text.Contains("Falha ao enviar notificação"))
        {
            return;
        }

        var message = logEvent.RenderMessage(_formatProvider);
        var exception = logEvent.Exception?.ToString() ?? "Nenhuma exceção detalhada.";

        
        if (exception.Length > 1900)
        {
            exception = exception.Substring(0, 1900);
        }

        var payload = new
        {
            content = "🚨 **Erro Crítico no Servidor** 🚨",
            embeds = new[]
            {
                new
                {
                    title = message,
                    description = $"```\n{exception}\n```",
                    color = 15158332, 
                    timestamp = logEvent.Timestamp.UtcDateTime
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            
            var response = _httpClient.PostAsync(_webhookUrl, content).Result;
        }
        catch
        {
            
        }
    }
}


public static class DiscordWebhookSinkExtensions
{
    public static LoggerConfiguration DiscordWebhook(
              this LoggerSinkConfiguration sinkConfiguration,
              string webhookUrl, 
              LogEventLevel restrictedToMinimumLevel = LogEventLevel.Error)
    {
        
        return sinkConfiguration.Sink(new DiscordWebhookSink(webhookUrl, null), restrictedToMinimumLevel);
    }
}
