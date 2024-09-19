

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
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
                    var agrus = $@" -E -S {server}   -i {dir}\backup.sql";
                    _logger.LogDebug($"Worker running: {agrus}");
                    //ProcessStartInfo info = new ProcessStartInfo($"sqlcmd", agrus);
                    ////  Indicades if the Operative System shell is used, in this case it is not
                    //info.UseShellExecute = false;
                    ////No new window is required
                    //info.CreateNoWindow = true;
                    ////The windows style will be hidden
                    //info.WindowStyle = ProcessWindowStyle.Hidden;
                    ////The output will be read by the starndar output process
                    //info.RedirectStandardOutput = true;
                    //Process proc = new Process();
                    //proc.StartInfo = info;
                    ////Start the process
                    //proc.Start();

                    string script = File.ReadAllText($@"{dir}\backup.sql");
                    SqlConnection conn = new SqlConnection(sqlConnection);
                    Server server1 = new Server(new ServerConnection(conn));
                    server1.ConnectionContext.ExecuteNonQuery(script);

                    _logger.LogDebug("Worker done!");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }
            }
        }
    }
}