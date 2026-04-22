namespace DevProject.Data.Entities
{
    using System.Text.Json.Nodes;

    public class JsonConversionResult
    {
        public required string SheetName { get; set; }
        public required string ApiId { get; set; }
        public required string ApiName { get; set; }
        public required string ResourceType { get; set; }
        public int TotalResources { get; set; }
        public required List<JsonNode> Resources { get; set; }
        public required ValidationReport Validation { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}