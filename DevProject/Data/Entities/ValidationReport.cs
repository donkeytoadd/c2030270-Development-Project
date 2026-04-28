namespace DevProject.Data.Entities
{
    public class ValidationReport
    {
        public bool IsValid { get; set; }
        public int TotalIssues { get; set; }
        public int ArtefactCount { get; set; }
        public double CompliancePercentage { get; set; } = 100.0;
        public List<ValidationIssue> Issues { get; set; } = new();
    }
}
