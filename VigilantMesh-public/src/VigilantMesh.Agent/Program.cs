
using VigilantMeshAgent;
using VigilantMeshAgent.Configuration;
using VigilantMeshAgent.Logging;
using VigilantMeshAgent.Services;
using VigilantMeshAgent.Security;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new ConsoleLogFormatter())
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "agent-boot-error-.log"),
        rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((_, configuration) =>
        {
            var protectedPath = Path.Combine(AppContext.BaseDirectory, "appsettings.protected.json");
            if (File.Exists(protectedPath))
            {
                configuration.Add(new ProtectedJsonConfigurationSource
                {
                    Path = protectedPath,
                    Optional = false
                });
            }
        })
        .UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(new ConsoleLogFormatter())
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VigilantMeshAgent", "logs", "agent-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information))

        .ConfigureServices((hostContext, services) =>
        {
            services.Configure<HeartbeatSettings>(
                hostContext.Configuration.GetSection(HeartbeatSettings.SectionName));

            
            
            services.AddHttpClient("HeartbeatClient", client => client.Timeout = TimeSpan.FromSeconds(15));
                

            services.AddSingleton<ISystemInfoService, SystemInfoService>();
            services.AddHostedService<Worker>();
        })
        .UseWindowsService(options =>
        {
            options.ServiceName = "VigilantMesh Agent";
        })
        .Build()
        .Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "O Host do Agente falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
