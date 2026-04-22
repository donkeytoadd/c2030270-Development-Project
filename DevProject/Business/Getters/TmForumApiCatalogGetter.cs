namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using DevProject.Business.Getters.Interfaces;
    using Microsoft.Extensions.Logging;

    public class TmForumApiCatalogGetter : ITmForumApiCatalogGetter
    {
        private readonly ITmForumGitHubRegistryGetter _registry;
        private readonly ILogger<TmForumApiCatalogGetter> _logger;
        private static readonly List<TmForumApiDefinition> BuiltInCatalog =
        [
            new() { ApiId = "TMF620", Name = "Product Catalog Management",    ResourceType = "ProductSpecification",  BasePath = "/productCatalogManagement/v4",    ResourceCollectionPath = "productSpecification",  Version = "4.0", MappingStrategy = MappingStrategy.Specification, BaseType = "EntitySpecification", CharacteristicArrayKey = "productSpecCharacteristic",  CharacteristicValueArrayKey = "productSpecCharacteristicValue"  },
            new() { ApiId = "TMF633", Name = "Service Catalog Management",    ResourceType = "ServiceSpecification",  BasePath = "/serviceCatalogManagement/v4",    ResourceCollectionPath = "serviceSpecification",  Version = "4.0", MappingStrategy = MappingStrategy.Specification, BaseType = "EntitySpecification", CharacteristicArrayKey = "serviceSpecCharacteristic",  CharacteristicValueArrayKey = "serviceSpecCharacteristicValue"  },
            new() { ApiId = "TMF634", Name = "Resource Catalog Management",   ResourceType = "ResourceSpecification", BasePath = "/resourceCatalogManagement/v4",   ResourceCollectionPath = "resourceSpecification", Version = "4.0", MappingStrategy = MappingStrategy.Specification, BaseType = "EntitySpecification", CharacteristicArrayKey = "resourceSpecCharacteristic", CharacteristicValueArrayKey = "resourceSpecCharacteristicValue" },
            new() { ApiId = "TMF637", Name = "Product Inventory Management",  ResourceType = "Product",               BasePath = "/productInventoryManagement/v4",  ResourceCollectionPath = "product",               Version = "4.0", MappingStrategy = MappingStrategy.Inventory, CharacteristicArrayKey = "productCharacteristic"  },
            new() { ApiId = "TMF638", Name = "Service Inventory Management",  ResourceType = "Service",               BasePath = "/serviceInventoryManagement/v4",  ResourceCollectionPath = "service",               Version = "4.0", MappingStrategy = MappingStrategy.Inventory, CharacteristicArrayKey = "serviceCharacteristic"  },
            new() { ApiId = "TMF639", Name = "Resource Inventory Management", ResourceType = "Resource",              BasePath = "/resourceInventoryManagement/v4", ResourceCollectionPath = "resource",              Version = "4.0", MappingStrategy = MappingStrategy.Inventory, CharacteristicArrayKey = "resourceCharacteristic" },
            new() { ApiId = "TMF622", Name = "Product Ordering Management",   ResourceType = "ProductOrder",          BasePath = "/productOrderingManagement/v4",   ResourceCollectionPath = "productOrder",           Version = "4.0", MappingStrategy = MappingStrategy.Order, CharacteristicArrayKey = "productOrderItem", OrderItemCharacteristicKey = "productCharacteristic", OrderItemNestedObjectKey = "product",  OrderItemNestedObjectType = "ProductRefOrValue"  },
            new() { ApiId = "TMF641", Name = "Service Ordering Management",   ResourceType = "ServiceOrder",          BasePath = "/serviceOrderingManagement/v4",   ResourceCollectionPath = "serviceOrder",           Version = "4.0", MappingStrategy = MappingStrategy.Order, CharacteristicArrayKey = "serviceOrderItem", OrderItemCharacteristicKey = "serviceCharacteristic", OrderItemNestedObjectKey = "service", OrderItemNestedObjectType = "ServiceRefOrValue" },
            new() { ApiId = "TMF629", Name = "Customer Management",           ResourceType = "Customer",              BasePath = "/customerManagement/v4",           ResourceCollectionPath = "customer",               Version = "4.0", MappingStrategy = MappingStrategy.FieldMapped, CharacteristicArrayKey = "characteristic"      },
            new() { ApiId = "TMF632", Name = "Party Management",              ResourceType = "Individual",            BasePath = "/partyManagement/v4",              ResourceCollectionPath = "individual",             Version = "4.0", MappingStrategy = MappingStrategy.FieldMapped, CharacteristicArrayKey = "partyCharacteristic" },
            new() { ApiId = "TMF651", Name = "Agreement Management",          ResourceType = "Agreement",             BasePath = "/agreementManagement/v4",          ResourceCollectionPath = "agreement",              Version = "4.0", MappingStrategy = MappingStrategy.FieldMapped, CharacteristicArrayKey = "characteristic"      },
            new() { ApiId = "TMF666", Name = "Account Management",            ResourceType = "BillingAccount",        BasePath = "/accountManagement/v4",            ResourceCollectionPath = "billingAccount",         Version = "4.0", MappingStrategy = MappingStrategy.FieldMapped, CharacteristicArrayKey = "characteristic"      },
            new() { ApiId = "FLAT",   Name = "Flat / Pass-through",           ResourceType = "Row",                   BasePath = "",                                 ResourceCollectionPath = "",                      Version = "–",   MappingStrategy = MappingStrategy.Raw },
        ];

        public TmForumApiCatalogGetter(
            ITmForumGitHubRegistryGetter registry,
            ILogger<TmForumApiCatalogGetter> logger)
        {
            _registry = registry;
            _logger   = logger;
        }

        public List<TmForumApiDefinition> GetAll()
        {
            try
            {
                var apis = Task.Run(() => _registry.GetAllApisAsync()).GetAwaiter().GetResult();
                if (apis.Count > 0)
                {
                    foreach (var api in apis)
                    {
                        var builtIn = BuiltInCatalog.FirstOrDefault(b =>
                            b.ApiId.Equals(api.ApiId, StringComparison.OrdinalIgnoreCase));
                        if (builtIn is not null)
                        {
                            api.MappingStrategy              = builtIn.MappingStrategy;
                            api.BaseType                     = builtIn.BaseType;
                            api.CharacteristicArrayKey       = builtIn.CharacteristicArrayKey;
                            api.CharacteristicValueArrayKey  = builtIn.CharacteristicValueArrayKey;
                            api.OrderItemCharacteristicKey   = builtIn.OrderItemCharacteristicKey;
                            api.OrderItemNestedObjectKey     = builtIn.OrderItemNestedObjectKey;
                            api.OrderItemNestedObjectType    = builtIn.OrderItemNestedObjectType;
                        }
                    }
                    return apis.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "GitHub registry unavailable — falling back to built-in catalog.");
            }
            return BuiltInCatalog;
        }

        public TmForumApiDefinition? GetById(string apiId)
        {
            return GetAll().FirstOrDefault(x =>
                x.ApiId.Equals(apiId, StringComparison.OrdinalIgnoreCase));
        }
    }
}