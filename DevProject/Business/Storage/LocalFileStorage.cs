namespace DevProject.Business.Storage
{
    using Interfaces;
    using Microsoft.AspNetCore.Hosting;

    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment env;
        private readonly ILogger<LocalFileStorage> logger;

        public LocalFileStorage(IWebHostEnvironment env, ILogger<LocalFileStorage> logger)
        {
            this.env    = env;
            this.logger = logger;
        }

        private string UploadsPath => Path.Combine(env.WebRootPath, "uploads");

        public async Task<string> SaveAsync(Stream stream, string fileExtension, CancellationToken ct)
        {
            var dir = UploadsPath;
            Directory.CreateDirectory(dir);

            var fileId   = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(dir, fileId);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs, ct);

            logger.LogInformation("Saved upload '{FileId}'.", fileId);
            return fileId;
        }

        public async Task<Stream> OpenReadAsync(string fileId, CancellationToken ct)
        {
            var filePath = Path.Combine(UploadsPath, fileId);
            var ms = new MemoryStream();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await fs.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }

        public Task<bool> ExistsAsync(string fileId, CancellationToken ct)
        {
            var exists = File.Exists(Path.Combine(UploadsPath, fileId));
            return Task.FromResult(exists);
        }

        public Task DeleteAsync(string fileId, CancellationToken ct)
        {
            var filePath = Path.Combine(UploadsPath, fileId);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }
    }
}