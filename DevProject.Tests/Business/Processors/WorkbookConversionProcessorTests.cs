namespace DevProject.Tests.Business.Processors
{
    using DevProject.Business.Processors;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class WorkbookConversionProcessorTests : TestBase<WorkbookConversionProcessor>
    {
        private static ParsedWorkbook BuildWorkbook(params ParsedExcelData[] sheets) =>
            new()
            {
                WorkbookName = "TestWorkbook",
                Sheets = sheets.ToList()
            };

        private static ParsedExcelData BuildSheet(
            string name,
            List<string> columns,
            List<Dictionary<string, CellValue>> rows) =>
            new()
            {
                SpreadsheetName = name,
                ColumnNames = columns,
                Rows = rows,
                TotalRows = rows.Count
            };

        private static MappingTemplate MinimalTemplate(List<SheetMapping> mappings) =>
            new()
            {
                ApiId = "TMF634",
                ApiName = "Resource Catalog Management",
                RootResourceType = "ResourceSpecification",
                RootBaseType = "EntitySpecification",
                BasePath = "/resourceCatalogManagement/v4",
                ResourceCollectionPath = "resourceSpecification",
                SheetMappings = mappings
            };

        [Fact]
        public void ConvertSetsIdHrefAtTypeAtBaseTypeAndLastUpdate()
        {
            var workbook = BuildWorkbook();
            var template = MinimalTemplate([]);

            var sut = CreateTestSubject();
            var result = sut.Convert(workbook, template);

            Assert.NotNull(result.Output["id"]?.GetValue<string>());
            Assert.StartsWith("/resourceCatalogManagement/v4/resourceSpecification/",
                result.Output["href"]!.GetValue<string>());
            Assert.Equal("ResourceSpecification", result.Output["@type"]!.GetValue<string>());
            Assert.Equal("EntitySpecification", result.Output["@baseType"]!.GetValue<string>());
            Assert.NotNull(result.Output["lastUpdate"]?.GetValue<string>());
        }

        [Fact]
        public void ConvertOmitsAtBaseTypeWhenTemplateHasNoRootBaseType()
        {
            var template = new MappingTemplate
            {
                ApiId = "TMF638",
                ApiName = "Service Inventory",
                RootResourceType = "Service",
                RootBaseType = null,
                BasePath = "/serviceInventoryManagement/v4",
                ResourceCollectionPath = "service",
                SheetMappings = []
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(), template);

            Assert.False(result.Output.ContainsKey("@baseType"));
        }

        [Fact]
        public void ConvertOmitsIdAndHrefWhenEmitIdAndEmitHrefAreFalse()
        {
            var template = new MappingTemplate
            {
                ApiId = "TMF633",
                ApiName = "Service Catalog Management",
                RootResourceType = "ServiceSpecification",
                BasePath = "/serviceCatalogManagement/v4",
                ResourceCollectionPath = "serviceSpecification",
                EmitId = false,
                EmitHref = false,
                EmitLastUpdate = false,
                SheetMappings = []
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(), template);

            Assert.False(result.Output.ContainsKey("id"));
            Assert.False(result.Output.ContainsKey("href"));
            Assert.False(result.Output.ContainsKey("lastUpdate"));
            Assert.Equal("ServiceSpecification", result.Output["@type"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertEmitsIdButNotHrefWhenOnlyEmitIdIsTrue()
        {
            var template = new MappingTemplate
            {
                ApiId = "TMF634",
                ApiName = "Resource Catalog Management",
                RootResourceType = "ResourceSpecification",
                BasePath = "/resourceCatalogManagement/v4",
                ResourceCollectionPath = "resourceSpecification",
                EmitId = true,
                EmitHref = false,
                SheetMappings = []
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(), template);

            Assert.True(result.Output.ContainsKey("id"));
            Assert.False(result.Output.ContainsKey("href"));
        }

        [Fact]
        public void ConvertSetsApiMetadataOnResult()
        {
            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(), MinimalTemplate([]));

            Assert.Equal("TestWorkbook", result.WorkbookName);
            Assert.Equal("TMF634", result.ApiId);
            Assert.Equal("Resource Catalog Management", result.ApiName);
            Assert.Equal("ResourceSpecification", result.ResourceType);
        }

        [Fact]
        public void ConvertTopLevelFieldsMergesFirstRowColumnsIntoRoot()
        {
            var sheet = BuildSheet("Overview",
                columns: ["Name", "Description", "Version"],
                rows:
                [
                    new()
                    {
                        ["Name"] = CellValue.FromText("WiFi Spec"),
                        ["Description"] = CellValue.FromText("A description"),
                        ["Version"] = CellValue.FromText("2.0")
                    }
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "Overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Description", TargetField = "description" },
                    new() { SourceColumn = "Version", TargetField = "version" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            Assert.Equal("WiFi Spec", result.Output["name"]!.GetValue<string>());
            Assert.Equal("A description", result.Output["description"]!.GetValue<string>());
            Assert.Equal("2.0", result.Output["version"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertTopLevelFieldsIgnoresColumnsNotInMappings()
        {
            var sheet = BuildSheet("Overview",
                columns: ["Name", "UnmappedColumn"],
                rows:
                [
                    new()
                    {
                        ["Name"] = CellValue.FromText("Spec"),
                        ["UnmappedColumn"]= CellValue.FromText("ignored")
                    }
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "Overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings = [new() { SourceColumn = "Name", TargetField = "name" }]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            Assert.False(result.Output.ContainsKey("UnmappedColumn"));
        }

        [Fact]
        public void ConvertTopLevelFieldsSkipsEmptyCells()
        {
            var sheet = BuildSheet("Overview",
                columns: ["Name", "Description"],
                rows:
                [
                    new()
                    {
                        ["Name"] = CellValue.FromText("Spec"),
                        ["Description"] = CellValue.Empty()
                    }
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "Overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Description", TargetField = "description" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            Assert.Equal("Spec", result.Output["name"]!.GetValue<string>());
            Assert.False(result.Output.ContainsKey("description"));
        }

        [Fact]
        public void ConvertTopLevelFieldsEmitsWarningWhenSheetMissing()
        {
            var mapping = new SheetMapping
            {
                SheetName = "MissingSheet",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings = [new() { SourceColumn = "Name", TargetField = "name" }]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(), MinimalTemplate([mapping]));

            Assert.Single(result.Warnings);
            Assert.Contains("MissingSheet", result.Warnings[0]);
        }

        [Fact]
        public void ConvertTopLevelFieldsEmitsWarningWhenSheetHasNoDataRows()
        {
            var sheet = BuildSheet("Overview", columns: ["Name"], rows: []);

            var mapping = new SheetMapping
            {
                SheetName = "Overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings = [new() { SourceColumn = "Name", TargetField = "name" }]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            Assert.Single(result.Warnings);
            Assert.Contains("no data rows", result.Warnings[0]);
        }

        [Fact]
        public void ConvertFlatArrayProducesOneArrayElementPerNonEmptyRow()
        {
            var sheet = BuildSheet("relatedParty",
                columns: ["Id", "Role"],
                rows:
                [
                    new() { ["Id"] = CellValue.FromText("P1"), ["Role"] = CellValue.FromText("Owner") },
                    new() { ["Id"] = CellValue.FromText("P2"), ["Role"] = CellValue.FromText("User") },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "relatedParty",
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = "relatedParty",
                ColumnMappings =
                [
                    new() { SourceColumn = "Id", TargetField = "id" },
                    new() { SourceColumn = "Role", TargetField = "role" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var array = result.Output["relatedParty"] as JsonArray;
            Assert.NotNull(array);
            Assert.Equal(2, array!.Count);
            Assert.Equal("P1", (array[0] as JsonObject)!["id"]!.GetValue<string>());
            Assert.Equal("Owner", (array[0] as JsonObject)!["role"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertFlatArraySkipsAllEmptyRows()
        {
            var sheet = BuildSheet("relatedParty",
                columns: ["Id", "Role"],
                rows:
                [
                    new() { ["Id"] = CellValue.FromText("P1"), ["Role"] = CellValue.FromText("Owner") },
                    new() { ["Id"] = CellValue.Empty(), ["Role"] = CellValue.Empty() },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "relatedParty",
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = "relatedParty",
                ColumnMappings =
                [
                    new() { SourceColumn = "Id", TargetField = "id" },
                    new() { SourceColumn = "Role", TargetField = "role" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var array = result.Output["relatedParty"] as JsonArray;
            Assert.Single(array!);
        }

        [Fact]
        public void ConvertFlatArrayPreservesTypedValues()
        {
            var sheet = BuildSheet("items",
                columns: ["Quantity", "IsActive"],
                rows:
                [
                    new()
                    {
                        ["Quantity"] = CellValue.FromNumber(5),
                        ["IsActive"] = CellValue.FromBoolean(true)
                    }
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "items",
                Pattern = SheetMappingPattern.FlatArray,
                TargetKey = "items",
                ColumnMappings =
                [
                    new() { SourceColumn = "Quantity", TargetField = "quantity" },
                    new() { SourceColumn = "IsActive", TargetField = "isActive" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var item = (result.Output["items"] as JsonArray)![0] as JsonObject;
            Assert.Equal(5, item!["quantity"]!.GetValue<double>());
            Assert.True( item!["isActive"]!.GetValue<bool>());
        }

        [Fact]
        public void ConvertGroupedNestedArrayGroupsRowsByGroupByColumnValue()
        {
            var sheet = BuildSheet("resourceSpecCharacteristic",
                columns: ["Name", "ValueType", "Value", "IsDefault"],
                rows:
                [
                    new() { ["Name"] = CellValue.FromText("Bandwidth"), ["ValueType"] = CellValue.FromText("number"), ["Value"] = CellValue.FromNumber(100), ["IsDefault"] = CellValue.FromBoolean(true) },
                    new() { ["Name"] = CellValue.FromText("Bandwidth"), ["ValueType"] = CellValue.FromText("number"), ["Value"] = CellValue.FromNumber(200), ["IsDefault"] = CellValue.FromBoolean(false) },
                    new() { ["Name"] = CellValue.FromText("Latency"), ["ValueType"] = CellValue.FromText("number"), ["Value"] = CellValue.FromNumber(5), ["IsDefault"] = CellValue.FromBoolean(true) },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "resourceSpecCharacteristic",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "resourceSpecCharacteristic",
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "ValueType", TargetField = "valueType" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                    new() { SourceColumn = "IsDefault", TargetField = "isDefault" },
                ],
                SubArrayDefinitions =
                [
                    new()
                    {
                        SubArrayKey = "resourceSpecCharacteristicValue",
                        SubArrayColumns = ["Value", "IsDefault"]
                    }
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var chars = result.Output["resourceSpecCharacteristic"] as JsonArray;
            Assert.NotNull(chars);
            Assert.Equal(2, chars!.Count);

            var bandwidth = chars[0] as JsonObject;
            Assert.Equal("Bandwidth", bandwidth!["name"]!.GetValue<string>());
            Assert.Equal("number", bandwidth!["valueType"]!.GetValue<string>());

            var bwValues = bandwidth!["resourceSpecCharacteristicValue"] as JsonArray;
            Assert.NotNull(bwValues);
            Assert.Equal(2, bwValues!.Count);
        }

        [Fact]
        public void ConvertGroupedNestedArraySetsParentFieldsFromFirstRowOnly()
        {
            var sheet = BuildSheet("resourceSpecCharacteristic",
                columns: ["Name", "ValueType", "Value"],
                rows:
                [
                    new() { ["Name"] = CellValue.FromText("Speed"), ["ValueType"] = CellValue.FromText("number"), ["Value"] = CellValue.FromNumber(10) },
                    new() { ["Name"] = CellValue.FromText("Speed"), ["ValueType"] = CellValue.FromText("CHANGED"), ["Value"] = CellValue.FromNumber(20) },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "resourceSpecCharacteristic",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "chars",
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "ValueType", TargetField = "valueType" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                ],
                SubArrayDefinitions =
                [
                    new() { SubArrayKey = "values", SubArrayColumns = ["Value"] }
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var char0 = (result.Output["chars"] as JsonArray)![0] as JsonObject; Assert.Equal("number", char0!["valueType"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertGroupedNestedArraySubArrayEntrySkippedWhenAllSubColumnsEmpty()
        {
            var sheet = BuildSheet("chars",
                columns: ["Name", "ValueType", "Value"],
                rows:
                [
                    new() { ["Name"] = CellValue.FromText("Speed"), ["ValueType"] = CellValue.FromText("number"), ["Value"] = CellValue.Empty() },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "chars",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "chars",
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "ValueType", TargetField = "valueType" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                ],
                SubArrayDefinitions =
                [
                    new() { SubArrayKey = "values", SubArrayColumns = ["Value"] }
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var valueArr = ((result.Output["chars"] as JsonArray)![0] as JsonObject)!["values"] as JsonArray;
            Assert.Empty(valueArr!);
        }

        [Fact]
        public void ConvertGroupedNestedArrayRowsWithEmptyGroupByColumnAreSkipped()
        {
            var sheet = BuildSheet("chars",
                columns: ["Name", "Value"],
                rows:
                [
                    new() { ["Name"] = CellValue.Empty(), ["Value"] = CellValue.FromText("orphan") },
                    new() { ["Name"] = CellValue.FromText("Speed"), ["Value"] = CellValue.FromText("100") },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "chars",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "chars",
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                ],
                SubArrayDefinitions =
                [
                    new() { SubArrayKey = "values", SubArrayColumns = ["Value"] }
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var chars = result.Output["chars"] as JsonArray;
            Assert.Single(chars!);
            Assert.Equal("Speed", (chars![0] as JsonObject)!["name"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertGroupedNestedArrayPreservesInsertionOrder()
        {
            var sheet = BuildSheet("chars",
                columns: ["Name", "Value"],
                rows:
                [
                    new() { ["Name"] = CellValue.FromText("Alpha"), ["Value"] = CellValue.FromText("a") },
                    new() { ["Name"] = CellValue.FromText("Beta"), ["Value"] = CellValue.FromText("b") },
                    new() { ["Name"] = CellValue.FromText("Alpha"), ["Value"] = CellValue.FromText("a2")},
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "chars",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "chars",
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                ],
                SubArrayDefinitions =
                [
                    new() { SubArrayKey = "vals", SubArrayColumns = ["Value"] }
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var chars = result.Output["chars"] as JsonArray;
            Assert.Equal(2, chars!.Count);
            Assert.Equal("Alpha", (chars[0] as JsonObject)!["name"]!.GetValue<string>());
            Assert.Equal("Beta", (chars[1] as JsonObject)!["name"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertGroupedNestedArrayMultipleSubArrayDefinitionsPopulatedIndependently()
        {
            var sheet = BuildSheet("chars",
                columns: ["Name", "Value", "RelatedName"],
                rows:
                [
                    new()
                    {
                        ["Name"] = CellValue.FromText("Speed"),
                        ["Value"] = CellValue.FromText("100"),
                        ["RelatedName"] = CellValue.FromText("Latency")
                    },
                    new()
                    {
                        ["Name"] = CellValue.FromText("Speed"),
                        ["Value"] = CellValue.FromText("200"),
                        ["RelatedName"] = CellValue.Empty()
                    },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "chars",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "chars",
                GroupByColumn = "Name",
                ColumnMappings =
                [
                    new() { SourceColumn = "Name", TargetField = "name" },
                    new() { SourceColumn = "Value", TargetField = "value" },
                    new() { SourceColumn = "RelatedName", TargetField = "relatedName" },
                ],
                SubArrayDefinitions =
                [
                    new() { SubArrayKey = "values", SubArrayColumns = ["Value"] },
                    new() { SubArrayKey = "relationships", SubArrayColumns = ["RelatedName"] },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var speed = (result.Output["chars"] as JsonArray)![0] as JsonObject;
            Assert.Equal(2, (speed!["values"] as JsonArray)!.Count);
            Assert.Single((speed!["relationships"] as JsonArray)!);
        }

        [Fact]
        public void ConvertParentChildNestedArrayLinksChildrenToParentByIdReference()
        {
            var sheet = BuildSheet("features",
                columns: ["Id", "Name", "ParentId"],
                rows:
                [
                    new() { ["Id"] = CellValue.FromText("F1"), ["Name"] = CellValue.FromText("WiFi"), ["ParentId"] = CellValue.Empty() },
                    new() { ["Id"] = CellValue.FromText("F2"), ["Name"] = CellValue.FromText("5GHz"), ["ParentId"] = CellValue.FromText("F1") },
                    new() { ["Id"] = CellValue.FromText("F3"), ["Name"] = CellValue.FromText("2.4GHz"),["ParentId"] = CellValue.FromText("F1") },
                    new() { ["Id"] = CellValue.FromText("F4"), ["Name"] = CellValue.FromText("Base"), ["ParentId"] = CellValue.Empty() },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "features",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "featureSpecification",
                ParentIdColumn = "Id",
                ChildParentRefColumn= "ParentId",
                ChildArrayKey = "featureSpecRelationship",
                ColumnMappings =
                [
                    new() { SourceColumn = "Id", TargetField = "id" },
                    new() { SourceColumn = "Name", TargetField = "name" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var features = result.Output["featureSpecification"] as JsonArray;
            Assert.NotNull(features);
            Assert.Equal(2, features!.Count);

            var wifi = features[0] as JsonObject;
            Assert.Equal("F1", wifi!["id"]!.GetValue<string>());
            Assert.Equal("WiFi", wifi!["name"]!.GetValue<string>());

            var children = wifi!["featureSpecRelationship"] as JsonArray;
            Assert.NotNull(children);
            Assert.Collection(children!, _ => { }, _ => { });

            var base_ = features[1] as JsonObject;
            Assert.Equal("F4", base_!["id"]!.GetValue<string>());
            Assert.False(base_!.ContainsKey("featureSpecRelationship"));
        }

        [Fact]
        public void ConvertParentChildNestedArrayChildWithUnknownParentRefIsDropped()
        {
            var sheet = BuildSheet("features",
                columns: ["Id", "Name", "ParentId"],
                rows:
                [
                    new() { ["Id"] = CellValue.FromText("F1"), ["Name"] = CellValue.FromText("Root"), ["ParentId"] = CellValue.Empty() },
                    new() { ["Id"] = CellValue.FromText("F2"), ["Name"] = CellValue.FromText("Orphan"), ["ParentId"] = CellValue.FromText("NONE") },
                ]);

            var mapping = new SheetMapping
            {
                SheetName = "features",
                Pattern = SheetMappingPattern.NestedArray,
                TargetKey = "featureSpecification",
                ParentIdColumn = "Id",
                ChildParentRefColumn = "ParentId",
                ChildArrayKey = "featureSpecRelationship",
                ColumnMappings =
                [
                    new() { SourceColumn = "Id", TargetField = "id" },
                    new() { SourceColumn = "Name", TargetField = "name" },
                ]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            var features = result.Output["featureSpecification"] as JsonArray;
            Assert.Single(features!);
        }

        [Fact]
        public void ConvertProcessesMultipleSheetsInOrder()
        {
            var overview = BuildSheet("Overview",
                columns: ["Name"],
                rows: [new() { ["Name"] = CellValue.FromText("My Spec") }]);

            var parties = BuildSheet("relatedParty",
                columns: ["Id", "Role"],
                rows: [new() { ["Id"] = CellValue.FromText("P1"), ["Role"] = CellValue.FromText("Owner") }]);

            var template = MinimalTemplate(
            [
                new SheetMapping
                {
                    SheetName = "Overview",
                    Pattern = SheetMappingPattern.TopLevelFields,
                    TargetKey = string.Empty,
                    ColumnMappings = [new() { SourceColumn = "Name", TargetField = "name" }]
                },
                new SheetMapping
                {
                    SheetName = "relatedParty",
                    Pattern = SheetMappingPattern.FlatArray,
                    TargetKey = "relatedParty",
                    ColumnMappings =
                    [
                        new() { SourceColumn = "Id", TargetField = "id" },
                        new() { SourceColumn = "Role", TargetField = "role" },
                    ]
                }
            ]);

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(overview, parties), template);

            Assert.Equal("My Spec", result.Output["name"]!.GetValue<string>());
            Assert.Single((result.Output["relatedParty"] as JsonArray)!);
        }

        [Fact]
        public void ConvertMatchesSheetNamesCaseInsensitively()
        {
            var sheet = BuildSheet("OVERVIEW",
                columns: ["Name"],
                rows: [new() { ["Name"] = CellValue.FromText("Spec") }]);

            var mapping = new SheetMapping
            {
                SheetName = "overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings = [new() { SourceColumn = "Name", TargetField = "name" }]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            Assert.Equal("Spec", result.Output["name"]!.GetValue<string>());
        }

        [Fact]
        public void ConvertEmitsNoWarningsWhenAllSheetsPresent()
        {
            var sheet = BuildSheet("Overview",
                columns: ["Name"],
                rows: [new() { ["Name"] = CellValue.FromText("Spec") }]);

            var mapping = new SheetMapping
            {
                SheetName = "Overview",
                Pattern = SheetMappingPattern.TopLevelFields,
                TargetKey = string.Empty,
                ColumnMappings = [new() { SourceColumn = "Name", TargetField = "name" }]
            };

            var sut = CreateTestSubject();
            var result = sut.Convert(BuildWorkbook(sheet), MinimalTemplate([mapping]));

            Assert.Empty(result.Warnings);
        }
    }
}
