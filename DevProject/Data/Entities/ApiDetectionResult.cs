namespace DevProject.Data.Entities
{
    public sealed class ApiDetectionResult
    {
        public required IReadOnlyList<ApiDetectionCandidate> Candidates { get; init; }

        public string? AutoSelectedApiId { get; init; }
    }
}
