namespace DevProject.Infrastructure
{
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    public class BlobStorageHealthCheck : IHealthCheck
    {
        private readonly BlobContainerClient containerClient;

        public BlobStorageHealthCheck(BlobContainerClient containerClient)
        {
            this.containerClient = containerClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await containerClient.ExistsAsync(cancellationToken);
                return response.Value
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Blob container does not exist.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Cannot reach blob storage.", ex);
            }
        }
    }
}
