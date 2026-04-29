namespace DevProject.Data.Entities
{ public sealed class SubArrayDefinition
    {
        public required string SubArrayKey { get; init; }
        public required List<string> SubArrayColumns { get; init; }
    }
}
