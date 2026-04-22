namespace DevProject.Data.Entities
{
    public class ValidationIssue
    {
        public int ResourceIndex { get; set; }
        public required string ResourceName { get; set; }
        public required string Field { get; set; }
        public required string Message { get; set; }
        public string Severity { get; set; } = "Error";
    }
}
