namespace DevProject.Workers
{
    using Microsoft.Extensions.Hosting; public class FileCleanupWorker : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<FileCleanupWorker> _logger;

        public FileCleanupWorker(
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<FileCleanupWorker> logger)
        {
            _env = env;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_config.GetValue("FileCleanup:Enabled", true))
                return;
            while (!stoppingToken.IsCancellationRequested)
            {
                CleanUp();
                await Task.Delay(CheckInterval, stoppingToken).ConfigureAwait(false);
            }
        }

        private void CleanUp()
        {
            var retentionHours = _config.GetValue<int>("FileCleanup:RetentionHours", 24);
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsPath))
                return;

            var cutoff = DateTime.UtcNow.AddHours(-retentionHours);
            var deleted = 0;

            foreach (var file in Directory.GetFiles(uploadsPath))
            {
                try
                {
                    if (File.GetCreationTimeUtc(file) < cutoff)
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "File cleanup: could not delete '{File}'.", file);
                }
            }

            if (deleted > 0)
                _logger.LogInformation("File cleanup: deleted {Count} expired upload(s) (>{RetentionHours}h old).",
                    deleted, retentionHours);
        }
    }
}
