namespace DevProject.Data.Entities
{
    using Enums;
    public sealed class SheetMapping
    {
        public required string SheetName { get; init; }
        public required SheetMappingPattern Pattern { get; init; }
        public string TargetKey { get; init; } = string.Empty;
        public required List<ColumnMapping> ColumnMappings { get; init; }
        public bool IsRequired { get; init; } = false;
        public string? GroupByColumn { get; init; }
        public List<SubArrayDefinition> SubArrayDefinitions { get; init; } = [];
        public string? ParentIdColumn { get; init; }
        public string? ChildParentRefColumn { get; init; }
        public string? ChildArrayKey { get; init; }
        public Dictionary<string, string> DropWarnings { get; init; } = [];
    }
}
