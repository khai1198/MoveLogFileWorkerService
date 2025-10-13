

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Diagnostics;

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
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var time = DateTime.Now;
          if (time.Hour == 17)
          {
            _isRunning = false;
            continue;
          }

          if (_isRunning)
          {
            continue;
          }

          if (time.Hour != 15)
          {
            continue;
          }
          _isRunning = true;
          var st = Stopwatch.StartNew();
          var script = File.ReadAllText($@"{dir}\backup.sql");
          _logger.LogInformation("Worker start: {0}", script);
          using var conn = new SqlConnection(sqlConnection);
          conn.Open();
          using var command = new SqlCommand(script, conn);
          command.CommandTimeout = 10800;
          await command.ExecuteNonQueryAsync(stoppingToken);
          //var server1 = new Server(new ServerConnection(conn));
          //server1.ConnectionContext.ExecuteNonQuery(script);
          conn.Close();
          _logger.LogInformation("Worker running ok: {0}", st.Elapsed.TotalMilliseconds);
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