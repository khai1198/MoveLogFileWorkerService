namespace MoveLogFileWorkerService
{
  public class Worker : BackgroundService
  {
    private bool _isRunning = false;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
      _logger = logger;
      _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var dir = _configuration.GetSection("CPath")?.Value ?? throw new Exception("Invalid CPath in config file");
      var server = _configuration.GetSection("Server")?.Value ?? throw new Exception("Invalid CPath in config file");
      var sqlConnection = _configuration.GetSection("ConnectionString")?.Value ?? throw new Exception("Invalid CPath in config file");
      _logger.LogInformation("Start");
      var service = new PostgreSqlBackupService(@"D:\Program Files\PostgreSQL\15\bin\pg_dump.exe");
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var time = DateTime.Now;
          _logger.LogInformation("Start backup");
          string result = await service.BackupAndUploadAsync(
                // PostgreSQL
                dbHost: "100.100.1.155",
                dbPort: 5432,
                database: "MexicoHSEReelTower",
                dbUsername: "postgres",
                dbPassword: "yourStrong123Password",

                // Local backup
                localFolder: @"D:\Backup",

                // FTP Server
                ftpHost: "100.100.1.22",
                ftpPort: 21,
                ftpUsername: "anonymous",
                ftpPassword: "123456",
                ftpFolder: "/backup/postgresql"
            );

          await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
          //_logger.LogInformation("Worker running ok: {0}", st.Elapsed.TotalMilliseconds);
          _logger.LogDebug("Worker done!");
        }
        catch (Exception ex)
        {
          _logger.LogError(ex.Message, ex);
        }
        finally
        {
          await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
      }
    }
  }
}