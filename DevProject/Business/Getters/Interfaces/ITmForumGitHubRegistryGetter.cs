namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;

    public interface ITmForumGitHubRegistryGetter
    {
        Task<IReadOnlyList<TmForumApiDefinition>> GetAllApisAsync(CancellationToken ct = default);        Task<string?> GetValidationSchemaAsync(string apiId, CancellationToken ct = default);
    }
}
