using MoveLogFileWorkerService;

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
            _logger.LogInformation("Start");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug($"Worker running {HelperConst.SourcePath}");
                    if (!Directory.Exists(HelperConst.SourcePath))
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        continue;
                    }
                    var dir = new DirectoryInfo(HelperConst.SourcePath);
                    var files = dir.GetFiles().OrderByDescending(x => x.LastWriteTime);
                    if (files.Count() < 3)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        continue;
                    }
                    var fileRemoves = files.Skip(2).ToList();
                    foreach (var item in fileRemoves)
                    {
                        item.Delete();
                    }
                    _logger.LogDebug($"Done");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }
            }
        }
    }
}