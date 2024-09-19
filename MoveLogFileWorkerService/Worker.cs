using MoveLogFileWorkerService;
using System.Diagnostics;

namespace MoveLogFileWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker running");
                    ProcessStartInfo info = new ProcessStartInfo("sqlcmd", @" -E -S localhost   -i C:\Users\khain\Downloads\backup.sql");
                    //  Indicades if the Operative System shell is used, in this case it is not
                    info.UseShellExecute = false;
                    //No new window is required
                    info.CreateNoWindow = true;
                    //The windows style will be hidden
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    //The output will be read by the starndar output process
                    info.RedirectStandardOutput = true;
                    Process proc = new Process();
                    proc.StartInfo = info;
                    //Start the process
                    proc.Start();
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }
            }
        }
    }
}