namespace DevProject.Data.Entities
{
    using System.Text.Json.Nodes;

    public sealed class WorkbookConversionResult
    {
        public required string WorkbookName { get; init; }
        public required string ApiId { get; init; }
        public required string ApiName { get; init; }
        public required string ResourceType { get; init; }
        public required JsonObject Output { get; init; }
        public ValidationReport Validation { get; init; } = new();
        public List<string> Warnings { get; init; } = [];
    }
}
