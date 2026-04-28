namespace DevProject.Data.Entities
{
    public sealed class ApiDetectionCandidate
    {
        public required string ApiId { get; init; }
        public required string ApiName { get; init; }

        public required int Confidence { get; init; }

        public required IReadOnlyList<string> MatchedSignals { get; init; }
        public required IReadOnlyList<string> MissingSignals { get; init; }
    }
}
