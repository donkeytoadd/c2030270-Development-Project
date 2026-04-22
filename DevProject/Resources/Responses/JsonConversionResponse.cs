namespace DevProject.Resources.Responses
{
    using Data.Entities;
    using System.Text.Json.Nodes;

    public class JsonConversionResponse
    {
        public required string FileId { get; set; }
        public required string SheetName { get; set; }
        public required string ApiId { get; set; }
        public required string ApiName { get; set; }
        public required string ResourceType { get; set; }
        public int TotalResources { get; set; }
        public required ValidationReport Validation { get; set; }
        public required List<JsonNode> Resources { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}