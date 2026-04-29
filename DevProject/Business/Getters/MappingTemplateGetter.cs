namespace DevProject.Business.Getters
{
    using Data.Entities;
    using Data.Enums;
    using Interfaces;

    public class MappingTemplateGetter : IMappingTemplateGetter
    {
        private static readonly List<MappingTemplate> BuiltInTemplates =
        [
            BuildTmf634(),
            BuildTmf633(),
            BuildTmf620(),
            BuildTmf637(),
            BuildTmf639(),
            BuildTmf629(),
            BuildTmf632(),
            BuildTmf622(),
            BuildTmf641(),
        ];

        public MappingTemplate? GetById(string apiId) =>
            BuiltInTemplates.FirstOrDefault(t =>
                t.ApiId.Equals(apiId, StringComparison.OrdinalIgnoreCase));

        public List<MappingTemplate> GetAll() => BuiltInTemplates;

        private static MappingTemplate BuildTmf634() => new()
        {
            ApiId = "TMF634",
            ApiName = "Resource Catalog Management",
            RootResourceType = "ResourceSpecification",
            RootBaseType = "EntitySpecification",
            BasePath = "/resourceCatalogManagement/v4",
            ResourceCollectionPath = "resourceSpecification",
            SheetMappings =
            [
                OverviewSheet(
                    extraMappings:
                    [
                        new() { SourceColumn = "Version", TargetField = "version" },
                        new() { SourceColumn = "IsBundle", TargetField = "isBundle" },
                        new() { SourceColumn = "LifecycleStatus", TargetField = "lifecycleStatus" },
                    ]),
                CharacteristicSheet(
                    sheetName: "resourceSpecCharacteristic",
                    charArrayKey: "resourceSpecCharacteristic",
                    valueArrayKey: "resourceSpecCharacteristicValue"),
                RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf633() => new()
        {
            ApiId = "TMF633",
            ApiName = "Service Catalog Management",
            RootResourceType = "ServiceSpecification",
            RootBaseType = null,
            BasePath = "/tmf-api/serviceCatalogManagement/v4",
            ResourceCollectionPath = "serviceSpecification",
            EmitId = false,
            EmitHref = false,
            EmitLastUpdate = false,
            SheetMappings =
            [
                new()
                {
                    SheetName = "Service",
                    Pattern = SheetMappingPattern.TopLevelFields,
                    TargetKey = string.Empty,
                    IsRequired = true,
                    ColumnMappings =
                    [
                        new() { SourceColumn = "name", TargetField = "name" },
                        new() { SourceColumn = "description", TargetField = "description" },
                        new() { SourceColumn = "version", TargetField = "version" },
                        new() { SourceColumn = "isBundle", TargetField = "isBundle" },
                        new() { SourceColumn = "lifecycleStatus", TargetField = "lifecycleStatus" },
                    ]
                },
                new()
                {
                    SheetName = "Category",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "category",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "id", TargetField = "id" },
                        new() { SourceColumn = "name", TargetField = "name" },
                        new() { SourceColumn = "type", TargetField = "@type" },
                        new() { SourceColumn = "referredType", TargetField = "@referredType" },
                    ]
                },
                new()
                {
                    SheetName = "RelatedParty",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "relatedParty",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "id", TargetField = "id" },
                        new() { SourceColumn = "role", TargetField = "role" },
                        new() { SourceColumn = "id", TargetField = "name" },
                        new() { TargetField = "@referredType", DefaultValue = "Organization" },
                        new() { TargetField = "@type", DefaultValue = "RelatedParty" },
                    ]
                },
                new()
                {
                    SheetName = "ServiceRelationship",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "serviceSpecRelationship",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "relationshipType", TargetField = "relationshipType" },
                        new() { SourceColumn = "id", TargetField = "id" },
                        new() { SourceColumn = "href", TargetField = "href" },
                        new() { SourceColumn = "name", TargetField = "name" },
                        new() { SourceColumn = "@referredType", TargetField = "@referredType" },
                    ]
                },
                new()
                {
                    SheetName = "SpecCharacteristic",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "specCharacteristic",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "id", TargetField = "id" },
                        new() { SourceColumn = "name", TargetField = "name" },
                        new() { SourceColumn = "description", TargetField = "description" },
                        new() { SourceColumn = "valueType", TargetField = "valueType" },
                        new() { SourceColumn = "minCardinality", TargetField = "minCardinality" },
                        new() { SourceColumn = "maxCardinality", TargetField = "maxCardinality" },
                        new() { SourceColumn = "isUnique", TargetField = "isUnique" },
                        new() { SourceColumn = "configurable", TargetField = "configurable" },
                        new() { SourceColumn = "characteristicValueSpecification", TargetField = "characteristicValueSpecification", ParseJson = true },
                        new() { SourceColumn = "charSpecRelationship", TargetField = "charSpecRelationship", ParseJson = true },
                    ]
                },
                new()
                {
                    SheetName = "FeatureSpecification",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "featureSpecification",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "id", TargetField = "id" },
                        new() { SourceColumn = "name", TargetField = "name" },
                        new() { SourceColumn = "description", TargetField = "description" },
                        new() { SourceColumn = "isEnabled", TargetField = "isEnabled" },
                        new() { SourceColumn = "isBundle", TargetField = "isBundle" },
                        new()
                        {
                            SourceColumn = "childFeatureIds",
                            TargetField = "featureSpecRelationship",
                            SplitToArray = new SplitToArrayConfig
                            {
                                Delimiter = ";",
                                ItemField = "featureId",
                                ConstantFields = new() { ["relationshipType"] = "includes" },
                                LookupMatchColumn = "id",
                                LookupResultColumn = "name",
                                LookupTargetField = "name",
                            },
                        },
                    ]
                },
            ]
        };

        private static MappingTemplate BuildTmf620() => new()
        {
            ApiId = "TMF620",
            ApiName = "Product Catalog Management",
            RootResourceType = "ProductSpecification",
            RootBaseType = "EntitySpecification",
            BasePath = "/productCatalogManagement/v4",
            ResourceCollectionPath = "productSpecification",
            SheetMappings =
            [
                OverviewSheet(
                    extraMappings:
                    [
                        new() { SourceColumn = "Version", TargetField = "version" },
                        new() { SourceColumn = "IsBundle", TargetField = "isBundle" },
                        new() { SourceColumn = "LifecycleStatus", TargetField = "lifecycleStatus" },
                        new() { SourceColumn = "Brand", TargetField = "brand" },
                    ]),
                CharacteristicSheet(
                    sheetName: "productSpecCharacteristic",
                    charArrayKey: "productSpecCharacteristic",
                    valueArrayKey: "productSpecCharacteristicValue"),
                RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf637() => new()
        {
            ApiId = "TMF637",
            ApiName = "Product Inventory Management",
            RootResourceType = "Product",
            BasePath = "/productInventoryManagement/v4",
            ResourceCollectionPath = "product",
            EmitLastUpdate = false,
            SheetMappings =
            [
                OverviewSheet(
                    extraMappings:
                    [
                        new() { SourceColumn = "Status", TargetField = "status" },
                    ]),
                FlatCharacteristicSheet("productCharacteristic"),
                RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf639() => new()
        {
            ApiId = "TMF639",
            ApiName = "Resource Inventory Management",
            RootResourceType = "Resource",
            BasePath = "/resourceInventoryManagement/v4",
            ResourceCollectionPath = "resource",
            SheetMappings =
            [
                OverviewSheet(
                    extraMappings:
                    [
                        new() { SourceColumn = "ResourceStatus", TargetField = "resourceStatus" },
                        new() { SourceColumn = "LifecycleStatus", TargetField = "lifecycleStatus" },
                        new() { SourceColumn = "Category", TargetField = "category" },
                    ]),
                FlatCharacteristicSheet("resourceCharacteristic"),
                RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf629() => new()
        {
            ApiId = "TMF629",
            ApiName = "Customer Management",
            RootResourceType = "Customer",
            BasePath = "/tmf-api/customerManagement/v5",
            ResourceCollectionPath = "customer",
            EmitLastUpdate = false,
            SheetMappings =
            [
                new()
                {
                    SheetName = "Overview",
                    Pattern = SheetMappingPattern.TopLevelFields,
                    TargetKey = string.Empty,
                    ColumnMappings =
                    [
                        new() { SourceColumn = "Name", TargetField = "name" },
                        new() { SourceColumn = "Status", TargetField = "status", LowercaseValue = true },
                        new() { SourceColumn = "Name", TargetField = "engagedParty.id" },
                        new() { SourceColumn = "Name", TargetField = "engagedParty.name" },
                        new() { TargetField = "engagedParty.@type", DefaultValue = "PartyRef" },
                        new() { TargetField = "engagedParty.@referredType", DefaultValue = "Organization" },
                    ]
                },
                Tmf629CharacteristicSheet(),
                V5RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf632() => new()
        {
            ApiId = "TMF632",
            ApiName = "Party Management",
            RootResourceType = "Individual",
            BasePath = "/partyManagement/v4",
            ResourceCollectionPath = "individual",
            SheetMappings =
            [
                OverviewSheet(
                    extraMappings:
                    [
                        new() { SourceColumn = "FamilyName", TargetField = "familyName" },
                        new() { SourceColumn = "GivenName", TargetField = "givenName" },
                        new() { SourceColumn = "Gender", TargetField = "gender" },
                        new() { SourceColumn = "Nationality", TargetField = "nationality" },
                    ]),
                FlatCharacteristicSheet("partyCharacteristic"),
                RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf622() => new()
        {
            ApiId = "TMF622",
            ApiName = "Product Ordering Management",
            RootResourceType = "ProductOrder",
            BasePath = "/productOrderingManagement/v4",
            ResourceCollectionPath = "productOrder",
            EmitLastUpdate = false,
            SheetMappings =
            [
                new()
                {
                    SheetName = "Overview",
                    Pattern = SheetMappingPattern.TopLevelFields,
                    TargetKey = string.Empty,
                    ColumnMappings =
                    [
                        new() { SourceColumn = "Description", TargetField = "description" },
                        new() { SourceColumn = "ExternalId", TargetField = "externalId" },
                        new() { SourceColumn = "Priority", TargetField = "priority" },
                        new() { SourceColumn = "RequestedStartDate", TargetField = "requestedStartDate" },
                        new() { SourceColumn = "RequestedCompletionDate", TargetField = "requestedCompletionDate" },
                        new() { SourceColumn = "State", TargetField = "state" },
                    ]
                },
                new SheetMapping
                {
                    SheetName = "productOrderItem",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "productOrderItem",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "Id", TargetField = "id" },
                        new() { SourceColumn = "Action", TargetField = "action" },
                        new() { SourceColumn = "Quantity", TargetField = "quantity" },
                        new() { SourceColumn = "ProductOfferingName", TargetField = "productOffering.name" },
                        new() { SourceColumn = "ProductOfferingId", TargetField = "productOffering.id" },
                    ]
                },
                RelatedPartySheet(),
            ]
        };

        private static MappingTemplate BuildTmf641() => new()
        {
            ApiId = "TMF641",
            ApiName = "Service Ordering Management",
            RootResourceType = "ServiceOrder",
            BasePath = "/tmf-api/serviceOrderingManagement/v5",
            ResourceCollectionPath = "serviceOrder",
            EmitLastUpdate = false,
            SheetMappings =
            [
                new()
                {
                    SheetName = "Overview",
                    Pattern = SheetMappingPattern.TopLevelFields,
                    TargetKey = string.Empty,
                    ColumnMappings =
                    [
                        new() { SourceColumn = "Description", TargetField = "description" },
                        new() { SourceColumn = "Priority", TargetField = "priority" },
                        new() { SourceColumn = "RequestedStartDate", TargetField = "requestedStartDate" },
                        new() { SourceColumn = "RequestedCompletionDate", TargetField = "requestedCompletionDate" },
                        new() { TargetField = "state", DefaultValue = "acknowledged" },
                    ]
                },
                new SheetMapping
                {
                    SheetName = "serviceOrderItem",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "serviceOrderItem",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "Id", TargetField = "id" },
                        new() { SourceColumn = "Action", TargetField = "action" },
                        new() { SourceColumn = "ServiceName", TargetField = "service.name" },
                        new() { SourceColumn = "ServiceId", TargetField = "service.id" },
                        new() { TargetField = "service.@type", DefaultValue = "ServiceRef" },
                        new() { TargetField = "service.@referredType", DefaultValue = "Service" },
                        new() { TargetField = "@type", DefaultValue = "ServiceOrderItem" },
                    ]
                },
                RelatedPartySheet(),
            ]
        };

        private static SheetMapping OverviewSheet(IEnumerable<ColumnMapping> extraMappings) =>
            new()
            {
                SheetName = "Overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings = CommonOverviewMappings()
                    .Concat(extraMappings)
                    .ToList()
            };

        private static IEnumerable<ColumnMapping> CommonOverviewMappings() =>
        [
            new() { SourceColumn = "Name", TargetField = "name" },
            new() { SourceColumn = "Description", TargetField = "description" },
        ];

        private static SheetMapping CharacteristicSheet(
            string sheetName,
            string charArrayKey,
            string valueArrayKey) =>
            new()
            {
                SheetName = sheetName,
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = charArrayKey,
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "ValueType", TargetField = "valueType" },
                    new() { SourceColumn = "MinCardinality", TargetField = "minCardinality" },
                    new() { SourceColumn = "MaxCardinality", TargetField = "maxCardinality" },
                    new() { SourceColumn = "IsUnique", TargetField = "isUnique" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                    new() { SourceColumn = "IsDefault", TargetField = "isDefault" },
                    new() { SourceColumn = "UnitOfMeasure", TargetField = "unitOfMeasure" },
                ],
                SubArrayDefinitions =
                [
                    new()
                    {
                        SubArrayKey = valueArrayKey,
                        SubArrayColumns = ["Value", "IsDefault", "UnitOfMeasure"]
                    }
                ]
            };

        private static SheetMapping FlatCharacteristicSheet(string charArrayKey) =>
            new()
            {
                SheetName = charArrayKey,
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = charArrayKey,
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                    new() { SourceColumn = "ValueType", TargetField = "valueType" },
                ]
            };

        private static SheetMapping Tmf629CharacteristicSheet() =>
            new()
            {
                SheetName = "characteristic",
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = "characteristic",
                DropWarnings = new()
                {
                    ["UnitOfMeasure"] = "Column 'UnitOfMeasure' on sheet 'characteristic' has no target field in the TMF629 schema and was not included in the output.",
                },
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "id" },
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                    new() { SourceColumn = "ValueType", TargetField = "valueType" },
                    new()
                    {
                        SourceColumn = "ValueType",
                        TargetField = "@type",
                        ValueMap = new()
                        {
                            ["string"] = "StringCharacteristic",
                            ["int"] = "IntegerCharacteristic",
                            ["integer"] = "IntegerCharacteristic",
                            ["decimal"] = "NumberCharacteristic",
                            ["number"] = "NumberCharacteristic",
                            ["date"] = "StringCharacteristic",
                            ["boolean"] = "BooleanCharacteristic",
                        }
                    },
                ]
            };

        private static SheetMapping RelatedPartySheet() =>
            new()
            {
                SheetName = "relatedParty",
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = "relatedParty",
                ColumnMappings =
                [
                    new() { SourceColumn = "Id", TargetField = "id" },
                    new() { SourceColumn = "Role", TargetField = "role" },
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "ReferredType", TargetField = "@referredType" },
                    new() { TargetField = "@type", DefaultValue = "RelatedParty" },
                ]
            };

        private static SheetMapping V5RelatedPartySheet() =>
            new()
            {
                SheetName = "relatedParty",
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = "relatedParty",
                ColumnMappings =
                [
                    new() { SourceColumn = "Id", TargetField = "id" },
                    new() { SourceColumn = "Role", TargetField = "role" },
                    new() { SourceColumn = "ReferredType", TargetField = "@referredType" },
                    new() { TargetField = "@referredType", DefaultValue = "Organization" },
                    new() { SourceColumn = "Id", TargetField = "partyOrPartyRole.id" },
                    new() { SourceColumn = "Name", TargetField = "partyOrPartyRole.name" },
                    new() { SourceColumn = "ReferredType", TargetField = "partyOrPartyRole.@referredType" },
                    new() { TargetField = "partyOrPartyRole.@referredType", DefaultValue = "Organization" },
                    new() { TargetField = "partyOrPartyRole.@type", DefaultValue = "PartyRef" },
                    new() { TargetField = "@type", DefaultValue = "RelatedPartyRefOrPartyRoleRef" },
                ]
            };
    }
}
