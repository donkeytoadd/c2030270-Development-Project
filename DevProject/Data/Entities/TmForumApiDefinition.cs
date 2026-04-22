namespace DevProject.Data.Entities
{
    using Data.Enums;

    public class TmForumApiDefinition
    {
        public required string ApiId { get; set; }
        public required string Name { get; set; }
        public required string ResourceType { get; set; }
        public required string BasePath { get; set; }
        public required string ResourceCollectionPath { get; set; }
        public required string Version { get; set; }
        public string CharacteristicArrayKey { get; set; } = "resourceSpecCharacteristic";
        public string CharacteristicValueArrayKey { get; set; } = "resourceSpecCharacteristicValue";        public string OrderItemCharacteristicKey { get; set; } = "characteristic";
        public string OrderItemNestedObjectKey { get; set; } = "product";        public string OrderItemNestedObjectType { get; set; } = "ProductRefOrValue";
        public MappingStrategy MappingStrategy { get; set; } = MappingStrategy.Specification;        public string? BaseType { get; set; }
    }
}