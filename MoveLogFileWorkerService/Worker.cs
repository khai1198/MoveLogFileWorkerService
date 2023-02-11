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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    _logger.LogInformation("Worker running");
                    var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    if (firstDayOfMonth == DateTime.Now.Date)
                    {
                        _logger.LogInformation("Start move file");
                        if (!Directory.Exists(HelperConst.SourcePath))
                        {
                            _logger.LogError($"SourcePath: {HelperConst.SourcePath} not exists!");
                            return;
                        }
                        Directory.CreateDirectory(HelperConst.TargetPath);
                        var files = Directory.GetFiles(HelperConst.SourcePath);
                        var dateTo = DateTime.Now.Date;
                        foreach (var file in files)
                        {
                            var extention = Path.GetExtension(file);
                            var fileName = Path.GetFileName(file);
                            if (extention == ".txt")
                            {
                                var date = new DateTime(2000 + int.Parse(fileName.Substring(0, 2)), int.Parse(fileName.Substring(2, 2)), int.Parse(fileName.Substring(4, 2))).Date;
                                if (date >= dateTo)
                                {
                                    continue;
                                }
                                var destinationFile = Path.Combine(HelperConst.TargetPath, fileName);
                                File.Move(file, destinationFile);
                                _logger.LogInformation("Move file in {pathFrom} to {pathTo}", file, destinationFile);
                            }
                            else if (extention == ".csv")
                            {
                                var date = new DateTime(int.Parse(fileName.Substring(0, 4)), int.Parse(fileName.Substring(4, 2)), int.Parse(fileName.Substring(6, 2))).Date;
                                if (date >= dateTo)
                                {
                                    continue;
                                }
                                var destinationFile = Path.Combine(HelperConst.TargetPath, fileName);
                                File.Move(file, destinationFile);
                                _logger.LogInformation("Move file in {pathFrom} to {pathTo}", file, destinationFile);
                            }
                        }
                        await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }
            }
        }
    }
}