namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class FieldMappedMappingGetterTests : TestBase<FieldMappedMappingGetter>
    {
        private static TmForumApiDefinition BuildApiDef() =>
            new TmForumApiDefinition
            {
                ApiId = "TMF632",
                Name = "Party Management",
                ResourceType = "Individual",
                BasePath = "/partyManagement/v4",
                ResourceCollectionPath = "individual",
                Version = "4.0",
                MappingStrategy = MappingStrategy.FieldMapped,
                CharacteristicArrayKey = "partyCharacteristic"
            };

        [Fact]
        public void SupportsFieldMappedStrategy()
        {
            var sut = CreateTestSubject();
            Assert.True(sut.Supports(MappingStrategy.FieldMapped));
            Assert.False(sut.Supports(MappingStrategy.Inventory));
        }

        [Fact]
        public void GetAllProducesOneResourcePerRow()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "FirstName" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["FirstName"] = CellValue.FromText("Alice") },
                    new() { ["FirstName"] = CellValue.FromText("Bob") }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllMapsConfiguredColumnsToTopLevelFields()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "FirstName", "LastName" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["FirstName"] = CellValue.FromText("Alice"),
                        ["LastName"] = CellValue.FromText("Smith")
                    }
                }
            };
            var config = new JsonConversionConfig
            {
                FieldMappings = new Dictionary<string, string>
                {
                    ["FirstName"] = "givenName",
                    ["LastName"] = "familyName"
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;

            Assert.Equal("Alice", obj!["givenName"]!.GetValue<string>());
            Assert.Equal("Smith", obj!["familyName"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllPutsUnmappedColumnsInCharacteristicBag()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "FirstName", "Department" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["FirstName"] = CellValue.FromText("Alice"),
                        ["Department"] = CellValue.FromText("Engineering")
                    }
                }
            };
            var config = new JsonConversionConfig
            {
                FieldMappings = new Dictionary<string, string> { ["FirstName"] = "givenName" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;
            var chars = obj!["partyCharacteristic"] as JsonArray;

            Assert.NotNull(chars);
            Assert.Single(chars!);
            Assert.Equal("Department", (chars![0] as JsonObject)!["name"]!.GetValue<string>());
        }

        [Fact]
        public void GetAllOmitsCharacteristicBagWhenAllColumnsAreMapped()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "FirstName" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["FirstName"] = CellValue.FromText("Alice") }
                }
            };
            var config = new JsonConversionConfig
            {
                FieldMappings = new Dictionary<string, string> { ["FirstName"] = "givenName" }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), config);
            var obj = result[0] as JsonObject;

            Assert.Null(obj!["partyCharacteristic"]);
        }

        [Fact]
        public void GetAllWithNoFieldMappingsTreatsAllColumnsAsCharacteristics()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "FirstName", "Department" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["FirstName"] = CellValue.FromText("Alice"),
                        ["Department"] = CellValue.FromText("Engineering")
                    }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig());
            var chars = (result[0] as JsonObject)!["partyCharacteristic"] as JsonArray;

            Assert.Equal(2, chars!.Count);
        }
    }
}
