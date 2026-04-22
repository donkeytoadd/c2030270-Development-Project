namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;

    public interface IMappingTemplateGetter
    {
        MappingTemplate? GetById(string apiId);
        List<MappingTemplate> GetAll();
    }
}
