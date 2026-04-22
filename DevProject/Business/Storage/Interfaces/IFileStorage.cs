namespace DevProject.Business.Storage.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream stream, string fileExtension, CancellationToken ct);
        Task<Stream> OpenReadAsync(string fileId, CancellationToken ct);

        Task<bool> ExistsAsync(string fileId, CancellationToken ct);

        Task DeleteAsync(string fileId, CancellationToken ct);
    }
}