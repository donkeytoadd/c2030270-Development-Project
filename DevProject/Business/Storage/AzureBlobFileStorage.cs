namespace DevProject.Business.Storage
{
    using Azure.Storage.Blobs;
    using Interfaces;

    public class AzureBlobFileStorage : IFileStorage
    {
        private readonly BlobContainerClient containerClient;
        private readonly ILogger<AzureBlobFileStorage> logger;
        private volatile bool containerEnsured = false;

        public AzureBlobFileStorage(BlobContainerClient containerClient, ILogger<AzureBlobFileStorage> logger)
        {
            this.containerClient = containerClient;
            this.logger = logger;
        }

        public async Task<string> SaveAsync(Stream stream, string fileExtension, CancellationToken ct)
        {
            await EnsureContainerAsync(ct);

            var fileId = $"{Guid.NewGuid()}{fileExtension}";
            var blob = containerClient.GetBlobClient(fileId);
            await blob.UploadAsync(stream, overwrite: true, ct);

            logger.LogInformation("Saved blob '{FileId}'.", fileId);
            return fileId;
        }

        public async Task<Stream> OpenReadAsync(string fileId, CancellationToken ct)
        {
            var blob = containerClient.GetBlobClient(fileId);
            var ms = new MemoryStream();
            await blob.DownloadToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }

        public async Task<bool> ExistsAsync(string fileId, CancellationToken ct)
        {
            var blob = containerClient.GetBlobClient(fileId);
            var response = await blob.ExistsAsync(ct);
            return response.Value;
        }

        public async Task DeleteAsync(string fileId, CancellationToken ct)
        {
            var blob = containerClient.GetBlobClient(fileId);
            await blob.DeleteIfExistsAsync(cancellationToken: ct);
        }

        private async Task EnsureContainerAsync(CancellationToken ct)
        {
            if (containerEnsured) return;
            await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
            containerEnsured = true;
        }
    }
}
