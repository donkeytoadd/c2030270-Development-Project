namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class SpecificationMappingGetterTests : TestBase<SpecificationMappingGetter>
    {
        private static TmForumApiDefinition BuildApiDef() =>
            new TmForumApiDefinition
            {
                ApiId = "TMF634",
                Name = "Resource Catalog Management",
                ResourceType = "ResourceSpecification",
                BasePath = "/resourceCatalogManagement/v4",
                ResourceCollectionPath = "resourceSpecification",
                Version = "4.0",
                MappingStrategy = MappingStrategy.Specification,
                BaseType = "EntitySpecification",
                CharacteristicArrayKey = "resourceSpecCharacteristic",
                CharacteristicValueArrayKey = "resourceSpecCharacteristicValue"
            };

        [Fact]
        public void SupportsSpecificationStrategy()
        {
            var sut = CreateTestSubject();
            Assert.True(sut.Supports(MappingStrategy.Specification));
            Assert.False(sut.Supports(MappingStrategy.Inventory));
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
                    new() { ["Name"] = CellValue.FromText("Alpha") },
                    new() { ["Name"] = CellValue.FromText("Beta") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllSetsAtTypeAndAtBaseTypeFromApiDefinition()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("Alpha") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;
            Assert.Equal("ResourceSpecification", obj!["@type"]!.GetValue<string>());
            Assert.Equal("EntitySpecification", obj!["@baseType"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllExcludesNameColumnFromCharacteristics()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Label", "Speed" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Label"] = CellValue.FromText("MySpec"),
                        ["Speed"] = CellValue.FromText("100 Mbps")
                    }
                }
            };
            var config = new JsonConversionConfig { NameColumn = "Label" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;
            var chars = obj!["resourceSpecCharacteristic"] as JsonArray;
            Assert.Equal("MySpec", obj!["name"]!.GetValue<string>()); Assert.Single(chars!);
            Assert.Equal("Speed", (chars![0] as JsonObject)!["name"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllAddsCardinalityDefaultsToCharacteristics()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Bandwidth" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Bandwidth"] = CellValue.FromNumber(100) }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var firstChar = ((result[0] as JsonObject)!["resourceSpecCharacteristic"] as JsonArray)![0] as JsonObject;

            Assert.Equal(0, firstChar!["minCardinality"]!.GetValue<int>());
            Assert.Equal(1, firstChar!["maxCardinality"]!.GetValue<int>());
            Assert.False( firstChar!["isUnique"]!.GetValue<bool>());
        }

        [Fact]
        public void GetAllUsesVersionColumnWhenConfigured()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name", "Ver" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Name"] = CellValue.FromText("MySpec"),
                        ["Ver"] = CellValue.FromText("2.3")
                    }
                }
            };
            var config = new JsonConversionConfig { NameColumn = "Name", VersionColumn = "Ver" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;

            Assert.Equal("2.3", obj!["version"]!.GetValue<string>()); var chars = obj!["resourceSpecCharacteristic"] as JsonArray;
            Assert.Empty(chars!);
        }

        [Fact]
        public void GetAllBuildsNestedCharacteristicValueArray()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Speed" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Speed"] = CellValue.FromNumber(100) }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;
            var chars = obj!["resourceSpecCharacteristic"] as JsonArray;

            Assert.NotNull(chars);
            Assert.Single(chars);

            var firstChar = chars![0] as JsonObject;
            var valueArray = firstChar!["resourceSpecCharacteristicValue"] as JsonArray;
            Assert.NotNull(valueArray);
            Assert.Single(valueArray);
            Assert.True(valueArray![0]!["isDefault"]!.GetValue<bool>());
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
                        ["Name"] = CellValue.FromText("Alpha"),
                        ["Notes"] = CellValue.Empty()
                    }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var chars = (result[0] as JsonObject)!["resourceSpecCharacteristic"] as JsonArray;

            Assert.Single(chars!);
            Assert.Equal("Name", (chars![0] as JsonObject)!["name"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllUsesNameColumnWhenConfigured()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Id", "Label" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Id"] = CellValue.FromText("42"),
                        ["Label"] = CellValue.FromText("MySpec")
                    }
                }
            };
            var config = new JsonConversionConfig { NameColumn = "Label" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);

            Assert.Equal("MySpec", (result[0] as JsonObject)!["name"]!.GetValue<string>());
        }

        [Theory]
        [InlineData(CellValueType.Number, "number")]
        [InlineData(CellValueType.Boolean, "boolean")]
        [InlineData(CellValueType.DateTime, "dateTime")]
        [InlineData(CellValueType.Text, "string")]
        public void GetAllMapsValueTypesCorrectly(CellValueType cellType, string expectedValueType)
        {
            var cell = cellType switch
            {
                CellValueType.Number => CellValue.FromNumber(1),
                CellValueType.Boolean => CellValue.FromBoolean(true),
                CellValueType.DateTime => CellValue.FromDateTime(DateTime.Now),
                _ => CellValue.FromText("test")
            };

            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Col" },
                Rows = new List<Dictionary<string, CellValue>> { new() { ["Col"] = cell } }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var firstChar = ((result[0] as JsonObject)!["resourceSpecCharacteristic"] as JsonArray)![0] as JsonObject;

            Assert.Equal(expectedValueType, firstChar!["valueType"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllAddsLifecycleStatusIsBundleAndLastUpdate()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("Alpha") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;

            Assert.Equal("Active", obj!["lifecycleStatus"]!.GetValue<string>());
            Assert.False(obj!["isBundle"]!.GetValue<bool>());
            Assert.NotNull(obj!["lastUpdate"]);
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
            var chars = (result[0] as JsonObject)!["resourceSpecCharacteristic"] as JsonArray;
            var firstChar = chars![0] as JsonObject;
            var valueArray = firstChar!["resourceSpecCharacteristicValue"] as JsonArray;
            var charValue = valueArray![0] as JsonObject;

            Assert.Equal("number", firstChar!["valueType"]!.GetValue<string>());
            Assert.Equal(100.0, charValue!["value"]!.GetValue<double>());
            Assert.Equal("Mbps", charValue!["unitOfMeasure"]!.GetValue<string>());
        }

        [Theory]
        [InlineData("10 GB")]
        [InlineData("1.5 km")]
        [InlineData("50 dBm")]
        public void GetAllParsesVariousUnitPatterns(string cellText)
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Col" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Col"] = CellValue.FromText(cellText) }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var chars = (result[0] as JsonObject)!["resourceSpecCharacteristic"] as JsonArray;
            var charValue = ((chars![0] as JsonObject)!["resourceSpecCharacteristicValue"] as JsonArray)![0] as JsonObject;

            Assert.Equal("number", (chars![0] as JsonObject)!["valueType"]!.GetValue<string>());
            Assert.NotNull(charValue!["unitOfMeasure"]);
        }

        [Fact]
        public void GetAllDoesNotParsePlainTextAsUnit()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Type" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Type"] = CellValue.FromText("Service Type") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var chars = (result[0] as JsonObject)!["resourceSpecCharacteristic"] as JsonArray;
            var charValue = ((chars![0] as JsonObject)!["resourceSpecCharacteristicValue"] as JsonArray)![0] as JsonObject;

            Assert.Equal("string", (chars![0] as JsonObject)!["valueType"]!.GetValue<string>());
            Assert.Null(charValue!["unitOfMeasure"]);
        }

        [Fact]
        public void GetAllSkipsBlankRows()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("Alpha") },
                    new() { ["Name"] = CellValue.Empty() },
                    new() { ["Name"] = CellValue.FromText("Beta") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());

            Assert.Equal(2, result.Count);
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
                        ["Name"] = CellValue.FromText("Alpha"),
                        ["Status"] = CellValue.FromText("Launched")
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

            Assert.Equal("Launched", obj!["lifecycleStatus"]!.GetValue<string>()); var chars = obj!["resourceSpecCharacteristic"] as JsonArray;
            Assert.Empty(chars!);
        }

        [Fact]
        public void GetAllDefaultsLifecycleStatusToActiveWhenColumnNotConfigured()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("Alpha") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;

            Assert.Equal("Active", obj!["lifecycleStatus"]!.GetValue<string>());
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
                    new() { ["Name"] = CellValue.FromText("Bundle A") }
                }
            };
            var config = new JsonConversionConfig { ResourceTypeOverride = "BundleProductSpecification" };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;

            Assert.Equal("BundleProductSpecification", obj!["@type"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllFallsBackToApiDefinitionTypeWhenOverrideNotSet()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Name"] = CellValue.FromText("Alpha") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var obj = result[0] as JsonObject;

            Assert.Equal("ResourceSpecification", obj!["@type"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllPromotesFieldMappingsToTopLevelAndExcludesFromCharacteristics()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Name", "OperState", "Speed" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Name"] = CellValue.FromText("Alpha"),
                        ["OperState"] = CellValue.FromText("inService"),
                        ["Speed"] = CellValue.FromText("100 Mbps")
                    }
                }
            };
            var config = new JsonConversionConfig
            {
                NameColumn = "Name",
                FieldMappings = new Dictionary<string, string> { ["OperState"] = "operationalState" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;
            var chars = obj!["resourceSpecCharacteristic"] as JsonArray;
            Assert.Equal("inService", obj!["operationalState"]!.GetValue<string>()); Assert.Single(chars!);
            Assert.Equal("Speed", (chars![0] as JsonObject)!["name"]!.GetValue<string>());
        }
    }
}
