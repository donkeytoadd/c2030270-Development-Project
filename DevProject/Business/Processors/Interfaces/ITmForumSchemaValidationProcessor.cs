namespace DevProject.Business.Processors.Interfaces
{
    using Data.Entities;
    using System.Text.Json.Nodes;

    public interface ITmForumSchemaValidationProcessor
    {
        ValidationReport Validate(List<JsonNode> resources, string apiId);
    }
}
