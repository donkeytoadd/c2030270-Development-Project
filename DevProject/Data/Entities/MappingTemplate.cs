namespace DevProject.Data.Entities
{
    public sealed class MappingTemplate
    {
        public required string ApiId { get; init; }

        public required string ApiName { get; init; }

        public required string RootResourceType { get; init; }

        public string? RootBaseType { get; init; }

        public required string BasePath { get; init; }

        public required string ResourceCollectionPath { get; init; }

        public required List<SheetMapping> SheetMappings { get; init; }        public bool EmitLastUpdate { get; init; } = true;
    }
}
