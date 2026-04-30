namespace DevProject.Tests.Business.Getters
{
    using DevProject.Business.Getters;
    using DevProject.Business.Getters.Interfaces;
    using DevProject.Data.Entities;
    using DevProject.Data.Enums;
    using Moq;
    using System.Text.Json.Nodes;

    public class TmForumResourceGetterTests : TestBase<TmForumResourceGetter>
    {
        private readonly Mock<IResourceMappingGetter> _mockStrategy;

        public TmForumResourceGetterTests()
        {
            _mockStrategy = new Mock<IResourceMappingGetter>();
            automocker.Use<IEnumerable<IResourceMappingGetter>>(new[] { _mockStrategy.Object });
        }

        [Fact]
        public void GetAllDelegatesToTheMatchingStrategy()
        {
            var apiDef = BuildApiDef(MappingStrategy.Specification);
            var data = BuildParsedData();
            var config = new JsonConversionConfig();
            var expected = new List<JsonNode> { new JsonObject { ["id"] = "1" } };

            _mockStrategy.Setup(x => x.Supports(MappingStrategy.Specification)).Returns(true);
            _mockStrategy.Setup(x => x.GetAll(data, apiDef, config)).Returns(expected);

            var sut = CreateTestSubject();
            var result = sut.GetAll(data, apiDef, config);

            Assert.Same(expected, result);
        }

        [Fact]
        public void GetAllThrowsWhenNoStrategySupportsTheMappingStrategy()
        {
            var apiDef = BuildApiDef(MappingStrategy.Order);
            _mockStrategy.Setup(x => x.Supports(It.IsAny<MappingStrategy>())).Returns(false);

            var sut = CreateTestSubject();

            Assert.Throws<InvalidOperationException>(
                () => sut.GetAll(BuildParsedData(), apiDef, new JsonConversionConfig()));
        }

        [Fact]
        public void GetAllPassesDataAndConfigUnmodifiedToStrategy()
        {
            var apiDef = BuildApiDef(MappingStrategy.Inventory);
            var data = BuildParsedData();
            var config = new JsonConversionConfig { NameColumn = "Name" };

            _mockStrategy.Setup(x => x.Supports(MappingStrategy.Inventory)).Returns(true);
            _mockStrategy.Setup(x => x.GetAll(It.IsAny<ParsedExcelData>(), It.IsAny<TmForumApiDefinition>(), It.IsAny<JsonConversionConfig>()))
                         .Returns(new List<JsonNode>());

            var sut = CreateTestSubject();
            sut.GetAll(data, apiDef, config);

            _mockStrategy.Verify(x => x.GetAll(data, apiDef, config), Times.Once);
        }

        private static TmForumApiDefinition BuildApiDef(MappingStrategy strategy) =>
            new TmForumApiDefinition
            {
                ApiId = "TMF634",
                Name = "Resource Catalog Management",
                ResourceType = "ResourceSpecification",
                BasePath = "/resourceCatalogManagement/v4",
                ResourceCollectionPath = "resourceSpecification",
                Version = "4.0",
                MappingStrategy = strategy
            };

        private static ParsedExcelData BuildParsedData() =>
            new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string> { "Id" },
                Rows = new List<Dictionary<string, CellValue>>()
            };
    }
}
