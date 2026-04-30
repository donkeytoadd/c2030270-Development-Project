namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class InventoryMappingGetterTests : TestBase<InventoryMappingGetter>
    {
        private static TmForumApiDefinition BuildApiDef() =>
            new TmForumApiDefinition
            {
                ApiId = "TMF639",
                Name = "Resource Inventory Management",
                ResourceType = "Resource",
                BasePath = "/resourceInventoryManagement/v4",
                ResourceCollectionPath = "resource",
                Version = "4.0",
                MappingStrategy = MappingStrategy.Inventory,
                CharacteristicArrayKey = "resourceCharacteristic"
            };

        [Fact]
        public void SupportsInventoryStrategy()
        {
            var sut = CreateTestSubject();
            Assert.True(sut.Supports(MappingStrategy.Inventory));
            Assert.False(sut.Supports(MappingStrategy.Specification));
        }

        [Fact]
        public void GetAllProducesOneResourcePerRow()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("RouterA") },
                    new() { ["Name"] = CellValue.FromText("RouterB") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllBuildsFlatCharacteristicsWithNoNestedValueArray()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Bandwidth" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Bandwidth"] = CellValue.FromNumber(1000) }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;
            var chars = obj!["resourceCharacteristic"] as JsonArray;

            Assert.NotNull(chars);
            var firstChar = chars![0] as JsonObject;
            Assert.NotNull(firstChar!["name"]);
            Assert.NotNull(firstChar["value"]);
            Assert.NotNull(firstChar["valueType"]);
            Assert.Null(firstChar["resourceSpecCharacteristicValue"]);
        }

        [Fact]
        public void GetAllDoesNotIncludeAtBaseType()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("RouterA") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;

            Assert.Null(obj!["@baseType"]);
        }

        [Fact]
        public void GetAllSkipsEmptyCells()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name", "Notes" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Name"] = CellValue.FromText("RouterA"),
                        ["Notes"] = CellValue.Empty()
                    }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var chars = (result[0] as JsonObject)!["resourceCharacteristic"] as JsonArray;

            Assert.Single(chars!);
        }

        [Fact]
        public void GetAllParsesTextCellWithUnitIntoNumericValueAndUnitOfMeasure()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Bandwidth" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Bandwidth"] = CellValue.FromText("100 Mbps") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var firstChar = ((result[0] as JsonObject)!["resourceCharacteristic"] as JsonArray)![0] as JsonObject;

            Assert.Equal("number", firstChar!["valueType"]!.GetValue<string>());
            Assert.Equal(100.0, firstChar!["value"]!.GetValue<double>());
            Assert.Equal("Mbps", firstChar!["unitOfMeasure"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllDoesNotEmitUnitOfMeasureForPlainText()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Location" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Location"] = CellValue.FromText("London DC") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var firstChar = ((result[0] as JsonObject)!["resourceCharacteristic"] as JsonArray)![0] as JsonObject;

            Assert.Equal("string", firstChar!["valueType"]!.GetValue<string>());
            Assert.Null(firstChar!["unitOfMeasure"]);
        }

        [Fact]
        public void GetAllUsesLifecycleStatusColumnWhenConfigured()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name", "Status" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Name"] = CellValue.FromText("RouterA"),
                        ["Status"] = CellValue.FromText("operating")
                    }
                }
            };
            var config = new JsonConversionConfig
            {
                NameColumn = "Name",
                LifecycleStatusColumn = "Status"
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;

            Assert.Equal("operating", obj!["lifecycleStatus"]!.GetValue<string>()); var chars = obj!["resourceCharacteristic"] as JsonArray;
            Assert.Null(chars);
        }

        [Fact]
        public void GetAllOmitsLifecycleStatusWhenColumnNotConfigured()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("RouterA") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;

            Assert.Null(obj!["lifecycleStatus"]);
        }

        [Fact]
        public void GetAllUsesResourceTypeOverrideForAtType()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("RouterA") }
                }
            };
            var config = new JsonConversionConfig { ResourceTypeOverride = "PhysicalResource" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;

            Assert.Equal("PhysicalResource", obj!["@type"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllPromotesFieldMappingsToTopLevelAndExcludesFromCharacteristics()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name", "Location", "Capacity" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Name"] = CellValue.FromText("RouterA"),
                        ["Location"] = CellValue.FromText("London"),
                        ["Capacity"] = CellValue.FromNumber(10)
                    }
                }
            };
            var config = new JsonConversionConfig
            {
                NameColumn = "Name",
                FieldMappings = new Dictionary<string, string> { ["Location"] = "place" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;
            var chars = obj!["resourceCharacteristic"] as JsonArray;
            Assert.Equal("London", obj!["place"]!.GetValue<string>()); Assert.Single(chars!);
            Assert.Equal("Capacity", (chars![0] as JsonObject)!["name"]!.GetValue<string>());
        }
    }
}
