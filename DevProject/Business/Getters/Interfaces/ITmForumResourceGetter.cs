namespace DevProject.Business.Getters.Interfaces
{
    using Data.Entities;
    using System.Text.Json.Nodes;

    public interface ITmForumResourceGetter
    {
        List<JsonNode> GetAll(ParsedExcelData data, TmForumApiDefinition apiDef, JsonConversionConfig config);
    }
}