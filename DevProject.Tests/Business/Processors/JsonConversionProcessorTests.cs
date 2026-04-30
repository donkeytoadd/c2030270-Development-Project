namespace DevProject.Tests.Business.Processors
{
    using DevProject.Business.Getters.Interfaces;
    using DevProject.Business.Processors;
    using DevProject.Business.Processors.Interfaces;
    using DevProject.Data.Entities;
    using Moq;
    using System.Text.Json.Nodes;

    public class JsonConversionProcessorTests : TestBase<JsonConversionProcessor>
    {
        private static TmForumApiDefinition DefaultApiDef() =>
            new TmForumApiDefinition
            {
                ApiId = "TMF634",
                Name = "Resource Catalog Management",
                ResourceType = "ResourceSpecification",
                BasePath = "/resourceCatalogManagement/v4",
                ResourceCollectionPath = "resourceSpecification",
                Version = "4.0"
            };

        private static JsonNode BuildStubResource() =>
            new JsonObject { ["id"] = Guid.NewGuid().ToString(), ["name"] = "Item" };

        private static ParsedExcelData BuildParsedData(int rowCount = 2) =>
            new ParsedExcelData
            {
                SpreadsheetName = "Products",
                ColumnNames = new List<string> { "Id", "Name" },
                Rows = Enumerable.Range(1, rowCount)
                    .Select(i => new Dictionary<string, CellValue>
                    {
                        ["Id"] = CellValue.FromNumber(i),
                        ["Name"] = CellValue.FromText($"Item {i}")
                    })
                    .ToList(),
                TotalRows = rowCount
            };

        private void SetupCatalogMock(TmForumApiDefinition? apiDef = null)
        {
            var def = apiDef ?? DefaultApiDef();
            this.automocker.GetMock<ITmForumApiCatalogGetter>()
                .Setup(x => x.GetById(It.IsAny<string>()))
                .Returns(def);
        }

        private void SetupResourceGetterMock(List<JsonNode>? resources = null)
        {
            this.automocker.GetMock<ITmForumResourceGetter>()
                .Setup(x => x.GetAll(
                    It.IsAny<ParsedExcelData>(),
                    It.IsAny<TmForumApiDefinition>(),
                    It.IsAny<JsonConversionConfig>()))
                .Returns(resources ?? new List<JsonNode>());
        }

        [Fact]
        public void ConvertDelegatesToResourceGetter()
        {
            var parsedData = BuildParsedData(3);
            var config = new JsonConversionConfig();
            var apiDef = DefaultApiDef();
            SetupCatalogMock(apiDef);

            var resources = Enumerable.Range(0, 3).Select(_ => BuildStubResource()).Cast<JsonNode>().ToList();
            this.automocker.GetMock<ITmForumResourceGetter>()
                .Setup(x => x.GetAll(It.IsAny<ParsedExcelData>(), It.IsAny<TmForumApiDefinition>(), It.IsAny<JsonConversionConfig>()))
                .Returns(resources);

            var sut = this.CreateTestSubject();
            sut.Convert(parsedData, config);

            this.automocker.GetMock<ITmForumResourceGetter>()
                .Verify(x => x.GetAll(parsedData, apiDef, config), Times.Once);
        }

        [Fact]
        public void ConvertReturnsTotalResourcesMatchingGetterOutput()
        {
            var parsedData = BuildParsedData(2);
            var config = new JsonConversionConfig();
            SetupCatalogMock();

            var resources = Enumerable.Range(0, 2).Select(_ => BuildStubResource()).Cast<JsonNode>().ToList();
            SetupResourceGetterMock(resources);

            var sut = this.CreateTestSubject();
            var result = sut.Convert(parsedData, config);

            Assert.Equal(2, result.TotalResources);
            Assert.Equal(2, result.Resources.Count);
        }

        [Fact]
        public void ConvertSetsSheetNameAndApiMetadataOnResult()
        {
            var parsedData = BuildParsedData(1);
            var config = new JsonConversionConfig { ApiId = "TMF633" };
            var apiDef = new TmForumApiDefinition
            {
                ApiId = "TMF633",
                Name = "Service Catalog Management",
                ResourceType = "ServiceSpecification",
                BasePath = "/serviceCatalogManagement/v4",
                ResourceCollectionPath = "serviceSpecification",
                Version = "4.0"
            };
            SetupCatalogMock(apiDef);
            SetupResourceGetterMock(new List<JsonNode> { BuildStubResource() });

            var sut = this.CreateTestSubject();
            var result = sut.Convert(parsedData, config);

            Assert.Equal("Products", result.SheetName);
            Assert.Equal("TMF633", result.ApiId);
            Assert.Equal("Service Catalog Management", result.ApiName);
            Assert.Equal("ServiceSpecification", result.ResourceType);
        }

        [Fact]
        public void ConvertIncludesWarningsFromRowValidator()
        {
            var parsedData = BuildParsedData(1);
            var config = new JsonConversionConfig();
            var expectedWarnings = new List<string> { "Row 1 is empty." };

            SetupCatalogMock();
            SetupResourceGetterMock(new List<JsonNode> { BuildStubResource() });

            this.automocker.GetMock<IJsonConversionRowValidationProcessor>()
                .Setup(x => x.Validate(It.IsAny<List<Dictionary<string, CellValue>>>(), It.IsAny<JsonConversionConfig>()))
                .Returns(expectedWarnings);

            var sut = this.CreateTestSubject();
            var result = sut.Convert(parsedData, config);

            Assert.Equal(expectedWarnings, result.Warnings);
        }

        [Fact]
        public void ConvertReturnsEmptyResourceListWhenGetterReturnsEmpty()
        {
            var parsedData = new ParsedExcelData
            {
                SpreadsheetName = "Empty",
                ColumnNames = new List<string>(),
                Rows = new List<Dictionary<string, CellValue>>()
            };
            var config = new JsonConversionConfig();
            SetupCatalogMock();
            SetupResourceGetterMock(new List<JsonNode>());

            var sut = this.CreateTestSubject();
            var result = sut.Convert(parsedData, config);

            Assert.Equal(0, result.TotalResources);
            Assert.Empty(result.Resources);
        }
    }
}
