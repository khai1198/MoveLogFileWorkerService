using Microsoft.Data.SqlClient;
using Npgsql;
using System.Diagnostics;

namespace MoveLogFileWorkerService
{
  public class Worker : BackgroundService
  {
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
      _logger = logger;
      _configuration = configuration;
    }

    public DateTime DateIndex { get; private set; } = default(DateTime);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      var dir = _configuration.GetSection("CPath")?.Value ?? throw new Exception("Invalid CPath in config file");
      var server = _configuration.GetSection("Server")?.Value ?? throw new Exception("Invalid CPath in config file");
      var sqlConnection = _configuration.GetSection("ConnectionString")?.Value ?? throw new Exception("Invalid CPath in config file");
      if (!int.TryParse(_configuration.GetSection("Period")?.Value, out int period))
      {
        period = 5;
      }

      _logger.LogInformation("Wait");
      await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
      _logger.LogInformation("Start");
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var date = DateTime.Now;
          if (date.Hour < 12 || date.Hour > 13 || DateIndex == date.Date)
          {
            continue;
          }
          var st = Stopwatch.StartNew();
          var script = File.ReadAllText($@"{dir}\index.sql");
          _logger.LogInformation("Worker start: {0}", script);

          using var conn = new NpgsqlConnection(sqlConnection);
          conn.Open();
          using var reindexCmd = new NpgsqlCommand(script, conn);
          reindexCmd.CommandTimeout = 0;
          reindexCmd.ExecuteNonQuery();
          conn.Close();

          _logger.LogInformation("Worker running ok: {0}", st.Elapsed.TotalMilliseconds);
          _logger.LogDebug("Worker done!");
          DateIndex = date.Date;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex.Message, ex);
        }
        finally
        {
          await Task.Delay(TimeSpan.FromSeconds(period), stoppingToken);
        }
      }
    }
  }
}