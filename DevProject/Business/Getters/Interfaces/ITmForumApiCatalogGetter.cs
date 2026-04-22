namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;

    public interface ITmForumApiCatalogGetter
    {
        List<TmForumApiDefinition> GetAll();
        TmForumApiDefinition? GetById(string apiId);
    }
}