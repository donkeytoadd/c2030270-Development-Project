namespace DevProject.Data.Entities
{
    public sealed class SplitToArrayConfig
    {
        public required string Delimiter { get; init; }

        public required string ItemField { get; init; }

        public Dictionary<string, string> ConstantFields { get; init; } = [];
    }
}
