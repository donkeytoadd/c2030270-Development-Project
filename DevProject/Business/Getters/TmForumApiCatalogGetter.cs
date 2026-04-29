namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using DevProject.Business.Getters.Interfaces;

    public class TmForumApiCatalogGetter : ITmForumApiCatalogGetter
    {
        private static readonly List<TmForumApiDefinition> BuiltInCatalog =
        [
            new() { ApiId = "TMF620", Name = "Product Catalog Management", ResourceType = "ProductSpecification", BasePath = "/productCatalogManagement/v4", ResourceCollectionPath = "productSpecification", Version = "4.0", MappingStrategy = MappingStrategy.Specification, BaseType = "EntitySpecification", CharacteristicArrayKey = "productSpecCharacteristic", CharacteristicValueArrayKey = "productSpecCharacteristicValue" },
            new() { ApiId = "TMF633", Name = "Service Catalog Management", ResourceType = "ServiceSpecification", BasePath = "/serviceCatalogManagement/v4", ResourceCollectionPath = "serviceSpecification", Version = "4.0", MappingStrategy = MappingStrategy.Specification, BaseType = "EntitySpecification", CharacteristicArrayKey = "serviceSpecCharacteristic", CharacteristicValueArrayKey = "serviceSpecCharacteristicValue" },
            new() { ApiId = "TMF634", Name = "Resource Catalog Management", ResourceType = "ResourceSpecification", BasePath = "/resourceCatalogManagement/v4", ResourceCollectionPath = "resourceSpecification", Version = "4.0", MappingStrategy = MappingStrategy.Specification, BaseType = "EntitySpecification", CharacteristicArrayKey = "resourceSpecCharacteristic", CharacteristicValueArrayKey = "resourceSpecCharacteristicValue" },
            new() { ApiId = "TMF637", Name = "Product Inventory Management", ResourceType = "Product", BasePath = "/productInventoryManagement/v4", ResourceCollectionPath = "product", Version = "4.0", MappingStrategy = MappingStrategy.Inventory, CharacteristicArrayKey = "productCharacteristic" },
            new() { ApiId = "TMF638", Name = "Service Inventory Management", ResourceType = "Service", BasePath = "/serviceInventoryManagement/v4", ResourceCollectionPath = "service", Version = "4.0", MappingStrategy = MappingStrategy.Inventory, CharacteristicArrayKey = "serviceCharacteristic" },
            new() { ApiId = "TMF639", Name = "Resource Inventory Management", ResourceType = "Resource", BasePath = "/resourceInventoryManagement/v4", ResourceCollectionPath = "resource", Version = "4.0", MappingStrategy = MappingStrategy.Inventory, CharacteristicArrayKey = "resourceCharacteristic" },
            new() { ApiId = "TMF622", Name = "Product Ordering Management", ResourceType = "ProductOrder", BasePath = "/productOrderingManagement/v4", ResourceCollectionPath = "productOrder", Version = "4.0", MappingStrategy = MappingStrategy.Order, CharacteristicArrayKey = "productOrderItem", OrderItemCharacteristicKey = "productCharacteristic", OrderItemNestedObjectKey = "product", OrderItemNestedObjectType = "ProductRefOrValue" },
            new() { ApiId = "TMF641", Name = "Service Ordering Management", ResourceType = "ServiceOrder", BasePath = "/serviceOrderingManagement/v4", ResourceCollectionPath = "serviceOrder", Version = "4.0", MappingStrategy = MappingStrategy.Order, CharacteristicArrayKey = "serviceOrderItem", OrderItemCharacteristicKey = "serviceCharacteristic", OrderItemNestedObjectKey = "service", OrderItemNestedObjectType = "ServiceRefOrValue" },
            new() { ApiId = "TMF629", Name = "Customer Management", ResourceType = "Customer", BasePath = "/customerManagement/v4", ResourceCollectionPath = "customer", Version = "4.0", MappingStrategy = MappingStrategy.FieldMapped, CharacteristicArrayKey = "characteristic" },
            new() { ApiId = "TMF632", Name = "Party Management", ResourceType = "Individual", BasePath = "/partyManagement/v4", ResourceCollectionPath = "individual", Version = "4.0", MappingStrategy = MappingStrategy.FieldMapped, CharacteristicArrayKey = "partyCharacteristic" },
            new() { ApiId = "FLAT", Name = "Flat / Pass-through", ResourceType = "Row", BasePath = "", ResourceCollectionPath = "", Version = "â€“", MappingStrategy = MappingStrategy.Raw },
        ];

        public List<TmForumApiDefinition> GetAll() => BuiltInCatalog;

        public TmForumApiDefinition? GetById(string apiId) =>
            BuiltInCatalog.FirstOrDefault(x =>
                x.ApiId.Equals(apiId, StringComparison.OrdinalIgnoreCase));
    }
}
