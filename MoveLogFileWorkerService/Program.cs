using MoveLogFileWorkerService;
using Serilog;
using Serilog.Events;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(config =>
    {
        config.ServiceName = "MoveLogFileWorkerService";
    })
    .ConfigureServices(services =>
    {
        services.AddLogging(x => x.AddSerilog());
        services.AddHostedService<Worker>();
    })
    .Build();

var configuration = host.Services.GetRequiredService<IConfiguration>();
HelperConst.SourcePath = configuration.GetSection("SourcePath")?.Value ?? throw new Exception("Invalid SourcePath in config file");
HelperConst.TargetPath = configuration.GetSection("TargetPath")?.Value ?? throw new Exception("Invalid TargetPath in config file");
var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
               .WriteTo.Async(c => c.File(Path.Combine(baseDirectory, "Logs", "logs.txt"), shared: true, rollingInterval: RollingInterval.Day))
               .CreateLogger();
Log.Information("Starting up");

await host.RunAsync();