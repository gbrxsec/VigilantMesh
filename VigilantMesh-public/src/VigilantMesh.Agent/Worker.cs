

using VigilantMeshAgent.Configuration;
using VigilantMeshAgent.Security; 
using VigilantMeshAgent.Services;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.Text;
using System.Text.Json;

namespace VigilantMeshAgent;

public record Heartbeat(
    string ClientId,
    string Hostname,
    string Username,
    string LocalIp,
    string Mac,
    bool VpnConnected,
    string? VpnIp,
    string? VpnAdapterName
);

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HeartbeatSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISystemInfoService _systemInfoService;
    private readonly string _clientId;

    public Worker(
        ILogger<Worker> logger,
        IOptions<HeartbeatSettings> settings,
        IHttpClientFactory httpClientFactory,
        ISystemInfoService systemInfoService)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _systemInfoService = systemInfoService;

        
        if (string.IsNullOrEmpty(_settings.ServerUrl) || string.IsNullOrEmpty(_settings.ApiKey))
        {
            var errorMsg = "ERRO CR�TICO: As configura��es 'ServerUrl' e/ou 'ApiKey' (usada como chave secreta HMAC) n�o foram encontradas na sec��o 'Heartbeat' do seu appsettings.json.";
            _logger.LogCritical(errorMsg);
            AnsiConsole.MarkupLine($"[bold red]{errorMsg}[/]");
            throw new ApplicationException(errorMsg);
        }

        _clientId = _systemInfoService.GetOrCreateClientId();
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        
        string banner = @"
 #####     ###    ######   ######    ####   ######    ###               ####   ##  ##   ######   ######     ###
#######   #####    ## ###  ### ###  ######   ######   ###              ######  ### ###   ######  ### ###   #####
###      ### ###   ## ###  ###  ##    ###    ##       ###                ###   ### ###   ##      ###  ##  ### ###
##  ###  ### ###   #####   ### ###    ###   ######   ###                 ###   #### ##   ######  ### ###  ### ###
##   ##  ### ###  ### ##   ######    ###    #####    ###                ###    ## ####  ######   ######   ### ###
### ###  #######  ### ###  ### ###   ###    ###      ### ###            ###    ### ###  ###      ### ###  #######
#######  ### ###  #######   ### ### #####   #######  #######           #####   ### ###  ####      ### ### ### ###
 ######   ##  ##  ######    ### ###  #####   #####    #####             #####   ##  ##   ###      ### ###  ##  ##
";
        AnsiConsole.MarkupLine($"[purple]{banner}[/]");
        AnsiConsole.MarkupLine("[grey]----------------------------------------------------------------------------------------------------------------------[/]");
        AnsiConsole.MarkupLine($"[bold white]ID do Agente:[/] [purple]{_clientId}[/]");
        AnsiConsole.MarkupLine($"[bold white]Servidor Target:[/] [purple]{_settings.ServerUrl}[/]");
        AnsiConsole.MarkupLine($"[bold white]Intervalo:[/] [purple]{_settings.IntervalSeconds} segundos[/]");
        AnsiConsole.MarkupLine("[grey]----------------------------------------------------------------------------------------------------------------------[/]");
        _logger.LogInformation("VigilantMesh Agent iniciado e pronto para operar.");

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var systemInfo = _systemInfoService.GetCurrentSystemInfo();

                var hb = new Heartbeat(
                    ClientId: _clientId,
                    Hostname: Environment.MachineName,
                    Username: Environment.UserName,
                    LocalIp: systemInfo.LocalIp ?? "N/A",
                    Mac: systemInfo.MacAddress ?? "N/A",
                    VpnConnected: systemInfo.IsVpnConnected,
                    VpnIp: systemInfo.VpnIpAddress,
                    VpnAdapterName: systemInfo.VpnAdapterName
                );

                await SendHeartbeatAsync(hb, stoppingToken);

                var status = systemInfo.IsVpnConnected
                    ? $"[bold green]CONECTADO[/] ([white]{systemInfo.VpnIpAddress ?? "IP n�o detectado"}[/])"
                    : "[yellow]DESCONECTADO[/]";
                AnsiConsole.MarkupLine($"[[{DateTime.Now:HH:mm:ss}]] [white]Status VPN:[/] {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro inesperado no ciclo do worker.");
                AnsiConsole.MarkupLine($"[[{DateTime.Now:HH:mm:ss}]] [bold red]ERRO:[/] Falha no ciclo de execu��o. Verifique o arquivo de log para detalhes.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.IntervalSeconds), stoppingToken);
        }
    }

    
    
    
    private async Task SendHeartbeatAsync(Heartbeat hb, CancellationToken stoppingToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("HeartbeatClient");

            var requestBody = JsonSerializer.Serialize(hb);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            
            var signature = HmacService.GenerateSignature(_settings.ApiKey, timestamp, requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, _settings.ServerUrl);

            
            request.Headers.Add("X-Request-Timestamp", timestamp);
            request.Headers.Add("X-Signature", signature);

            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao enviar heartbeat. Servidor respondeu com: {statusCode}", response.StatusCode);
                AnsiConsole.MarkupLine($"[[{DateTime.Now:HH:mm:ss}]] [yellow]AVISO:[/] Falha ao enviar dados. C�digo: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de rede ao conectar com o servidor de heartbeat.");
            AnsiConsole.MarkupLine($"[[{DateTime.Now:HH:mm:ss}]] [bold red]ERRO:[/] N�o foi poss�vel conectar ao servidor. Verifique a rede e a URL.");
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Envio do heartbeat cancelado devido ao desligamento do servi�o.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro inesperado ao enviar o heartbeat.");
            AnsiConsole.MarkupLine($"[[{DateTime.Now:HH:mm:ss}]] [bold red]ERRO:[/] Falha inesperada ao enviar dados. Verifique os logs.");
        }
    }
}
