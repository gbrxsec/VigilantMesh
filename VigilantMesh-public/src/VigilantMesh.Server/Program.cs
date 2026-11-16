

using VigilantMeshServer.Middleware;
using VigilantMeshServer.Logging;
using VigilantMeshServer.Models;
using VigilantMeshServer.Services;
using Serilog;
using Spectre.Console;
using Microsoft.AspNetCore.RateLimiting; 
using System.Threading.RateLimiting;      

public class Program
{
    public static void Main(string[] args)
    {
        
        string banner = @"
$$\                          $$$$$$\            $$\                 $$\           $$\ 
$$ |                        $$  __$$\           $$ |                \__|          $$ |
$$$$$$$\  $$\   $$\         $$ /  \__| $$$$$$\  $$$$$$$\   $$$$$$\  $$\  $$$$$$\  $$ |
$$  __$$\ $$ |  $$ |$$$$$$\ $$ |$$$$\  \____$$\ $$  __$$\ $$  __$$\ $$ |$$  __$$\ $$ |
$$ |  $$ |$$ |  $$ |\______|$$ |\_$$ | $$$$$$$ |$$ |  $$ |$$ |  \__|$$ |$$$$$$$$ |$$ |
$$ |  $$ |$$ |  $$ |        $$ |  $$ |$$  __$$ |$$ |  $$ |$$ |      $$ |$$   ____|$$ |
$$$$$$$  |\$$$$$$$ |        \$$$$$$  |\$$$$$$$ |$$$$$$$  |$$ |      $$ |\$$$$$$$\ $$ |
\_______/  \____$$ |         \______/  \_______|\_______/ \__|      \__| \_______|\__|
          $$\   $$ |                                                                  
          \$$$$$$  |                                                                  
           \______/                                                                                                                
";
        AnsiConsole.MarkupLine($"[cyan1]{banner}[/]");
        AnsiConsole.MarkupLine("[grey]--------------------------------------------------------------------------------[/]");
        AnsiConsole.MarkupLine("[bold white]VigilantMesh Server - Inicializando...[/]");
        AnsiConsole.MarkupLine("[grey]--------------------------------------------------------------------------------[/]");

        var builder = WebApplication.CreateBuilder(args);

        
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.Console();

            var alertWebhookUrl = context.Configuration["Discord:WebhookUrlAlerts"];
            if (!string.IsNullOrEmpty(alertWebhookUrl))
            {
                loggerConfiguration.WriteTo.DiscordWebhook(alertWebhookUrl);
            }
        });

        
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IHeartbeatStore>(sp => new InMemoryHeartbeatStore(TimeSpan.FromMinutes(30)));
        builder.Services.AddSingleton<DiscordNotificationService>();
        builder.Services.AddSingleton<ClientStateService>();
        builder.Services.AddHostedService<LifetimeNotificationService>();

        
        
        
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("fixed", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 30,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));
        });
        

        var app = builder.Build();

        
        app.UseSerilogRequestLogging();

        
        
        
        
        app.UseRateLimiter();
        

        app.UseMiddleware<HmacValidationMiddleware>();
        app.UseRouting();

        
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
           .WithTags("Manutenção");

        app.MapPost("/heartbeat", async (Heartbeat hb, IHeartbeatStore store, ClientStateService stateService) =>
        {
            if (string.IsNullOrWhiteSpace(hb.ClientId)) return Results.BadRequest();
            var processedHb = hb with { Timestamp = DateTime.UtcNow };
            store.Add(processedHb);
            await stateService.CheckAndNotifyStateChangeAsync(processedHb);
            return Results.Ok();
        })
        .WithTags("API")
        
        
        
        .RequireRateLimiting("fixed");
        

        
        AnsiConsole.MarkupLine("[green]Configuração concluída. Iniciando o servidor web...[/]");

        app.Run();
    }
}
