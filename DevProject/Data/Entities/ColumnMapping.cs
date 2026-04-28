namespace DevProject.Data.Entities
{
    public sealed class ColumnMapping
    {
        public string SourceColumn { get; init; } = string.Empty;
        public required string TargetField { get; init; }
        public bool ParseJson { get; init; }
        public string? DefaultValue { get; init; }
        public SplitToArrayConfig? SplitToArray { get; init; }
        public bool LowercaseValue { get; init; }
        public Dictionary<string, string>? ValueMap { get; init; }
    }
}
