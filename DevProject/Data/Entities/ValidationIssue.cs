namespace DevProject.Data.Entities
{
    using System.Text.Json.Serialization;
    using DevProject.Data.Enums;

    public class ValidationIssue
    {
        public int ResourceIndex { get; set; }
        public required string ResourceName { get; set; }
        public required string Field { get; set; }
        public required string Message { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    }
}
