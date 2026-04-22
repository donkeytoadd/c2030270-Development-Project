namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;
    using Data.Enums;
    using System.Text.Json.Nodes;

    public interface IResourceMappingGetter
    {
        bool Supports(MappingStrategy strategy);
        List<JsonNode> GetAll(ParsedExcelData data, TmForumApiDefinition apiDef, JsonConversionConfig config);
    }
}