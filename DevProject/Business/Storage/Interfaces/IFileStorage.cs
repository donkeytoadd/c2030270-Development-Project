namespace DevProject.Business.Storage.Interfaces
{
    public interface IFileStorage
    {
        /// <summary>
        /// Saves <paramref name="stream"/> with the given extension and returns the generated
        /// file identifier (GUID + extension, e.g. "3f2a...c1.xlsx").
        /// </summary>
        Task<string> SaveAsync(Stream stream, string fileExtension, CancellationToken ct);

        /// <summary>
        /// Opens the file identified by <paramref name="fileId"/> and returns a seekable stream.
        /// ClosedXML requires a seekable stream; implementations that do not naturally provide one
        /// must buffer into a MemoryStream before returning.
        /// </summary>
        Task<Stream> OpenReadAsync(string fileId, CancellationToken ct);

        Task<bool> ExistsAsync(string fileId, CancellationToken ct);

        Task DeleteAsync(string fileId, CancellationToken ct);
    }
}