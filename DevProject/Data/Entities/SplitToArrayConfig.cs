namespace DevProject.Data.Entities
{
    public sealed class SplitToArrayConfig
    {
        public required string Delimiter { get; init; }

        public required string ItemField { get; init; }

        public Dictionary<string, string> ConstantFields { get; init; } = [];
        
        public string? LookupMatchColumn { get; init; }
        public string? LookupResultColumn { get; init; }
        public string? LookupTargetField { get; init; }
    }
}
