namespace DevProject.Data.Entities
{
    public sealed class SplitToArrayConfig
    {
        public required string Delimiter { get; init; }

        public required string ItemField { get; init; }

        public Dictionary<string, string> ConstantFields { get; init; } = [];

        // Cross-row lookup: for each split item, find the row in the same sheet where
        // LookupMatchColumn == item value, then write that row's LookupResultColumn
        // into LookupTargetField on the output item.
        public string? LookupMatchColumn  { get; init; }
        public string? LookupResultColumn { get; init; }
        public string? LookupTargetField  { get; init; }
    }
}
