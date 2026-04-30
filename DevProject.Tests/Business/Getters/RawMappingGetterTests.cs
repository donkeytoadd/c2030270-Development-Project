namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using System.Text.Json.Nodes;

    public class RawMappingGetterTests : TestBase<RawMappingGetter>
    {
        private static TmForumApiDefinition BuildApiDef() =>
            new TmForumApiDefinition
            {
                ApiId = "FLAT",
                Name = "Flat / Pass-through",
                ResourceType = "Row",
                BasePath = "",
                ResourceCollectionPath = "",
                Version = "â€“",
                MappingStrategy = MappingStrategy.Raw
            };

        [Fact]
        public void SupportsRawStrategy()
        {
            var sut = CreateTestSubject();
            Assert.True(sut.Supports(MappingStrategy.Raw));
            Assert.False(sut.Supports(MappingStrategy.Specification));
            Assert.False(sut.Supports(MappingStrategy.Order));
        }

        [Fact]
        public void GetAllProducesOneObjectPerNonEmptyRow()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Field Name", "Data Type" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new() { ["Field Name"] = CellValue.FromText("id"), ["Data Type"] = CellValue.FromText("string") },
                    new() { ["Field Name"] = CellValue.Empty(), ["Data Type"] = CellValue.Empty() }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig { ApiId = "FLAT" });
            Assert.Single(result);
        }

        [Fact]
        public void GetAllMapsColumnNamesDirectlyToKeys()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "TMF622 Schema",
                ColumnNames = new List<string> { "Field Name", "JSON Path", "Data Type", "Required" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Field Name"] = CellValue.FromText("id"),
                        ["JSON Path"] = CellValue.FromText("$.id"),
                        ["Data Type"] = CellValue.FromText("string"),
                        ["Required"] = CellValue.FromText("R")
                    }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig { ApiId = "FLAT" });

            var obj = Assert.IsType<JsonObject>(result[0]);
            Assert.Equal("id", obj["Field Name"]?.GetValue<string>());
            Assert.Equal("$.id", obj["JSON Path"]?.GetValue<string>());
            Assert.Equal("string", obj["Data Type"]?.GetValue<string>());
            Assert.Equal("R", obj["Required"]?.GetValue<string>());
        }

        [Fact]
        public void GetAllDoesNotAddTmForumEnvelopeFields()
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
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig { ApiId = "FLAT" });

            var obj = Assert.IsType<JsonObject>(result[0]);
            Assert.False(obj.ContainsKey("id"), "Raw output must not contain 'id' envelope field");
            Assert.False(obj.ContainsKey("href"), "Raw output must not contain 'href' envelope field");
            Assert.False(obj.ContainsKey("@type"), "Raw output must not contain '@type' envelope field");
        }

        [Fact]
        public void GetAllPreservesTypedValues()
        {
            var data = new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Qty", "Active" },
                Rows = new List<Dictionary<string, CellValue>>
                {
                    new()
                    {
                        ["Qty"] = CellValue.FromNumber(42),
                        ["Active"] = CellValue.FromBoolean(true)
                    }
                }
            };

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig { ApiId = "FLAT" });

            var obj = Assert.IsType<JsonObject>(result[0]);
            Assert.Equal(42.0, obj["Qty"]?.GetValue<double>());
            Assert.True(obj["Active"]?.GetValue<bool>());
        }

        [Fact]
        public void GetAllOmitsEmptyCellsFromOutput()
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
            var result = sut.GetAll(data, BuildApiDef(), new JsonConversionConfig { ApiId = "FLAT" });

            var obj = Assert.IsType<JsonObject>(result[0]);
            Assert.True(obj.ContainsKey("Name"));
            Assert.False(obj.ContainsKey("Notes"), "Empty cells must not appear in output");
        }
    }
}
