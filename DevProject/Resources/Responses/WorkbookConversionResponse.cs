namespace DevProject.Resources.Responses
{
    using Data.Entities;
    using System.Text.Json.Nodes;

    public class WorkbookConversionResponse
    {
        public required string FileId { get; set; } public required string ResultId { get; set; }

        public required string WorkbookName { get; set; }
        public required string ApiId { get; set; }
        public required string ApiName { get; set; }
        public required string ResourceType { get; set; }
        public required JsonObject Output { get; set; }
        public ValidationReport Validation { get; set; } = new();
        public List<string> Warnings { get; set; } = [];
    }
}
