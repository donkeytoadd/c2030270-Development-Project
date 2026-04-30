namespace DevProject.Tests.Controllers
{
    using DevProject.Business.Getters.Interfaces;
    using DevProject.Business.Processors.Interfaces;
    using DevProject.Business.Storage.Interfaces;
    using DevProject.Controllers;
    using DevProject.Data.Entities;
    using DevProject.Data.Exceptions;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using Resources.Requests;
    using System.Text.Json;

    public class JsonConversionControllerTests : TestBase<JsonConversionController>
    {
        private const string ExistingFileId = "abc123.xlsx";

        public JsonConversionControllerTests()
        {
            automocker.GetMock<IFileStorage>()
                .Setup(x => x.ExistsAsync(ExistingFileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            automocker.GetMock<IFileStorage>()
                .Setup(x => x.OpenReadAsync(ExistingFileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream());
        }

        private static ParsedExcelData EmptyParsedData() =>
            new ParsedExcelData
            {
                SpreadsheetName = "Sheet1",
                ColumnNames = new List<string>(),
                Rows = new List<Dictionary<string, CellValue>>()
            };

        private static ParsedWorkbook EmptyParsedWorkbook() =>
            new ParsedWorkbook
            {
                WorkbookName = "test",
                Sheets = new List<ParsedExcelData> { EmptyParsedData() }
            };

        private static JsonConversionResult EmptyConversionResult() =>
            new JsonConversionResult
            {
                SheetName = "Sheet1",
                ApiId = "TMF634",
                ApiName = "Resource Catalog Management",
                ResourceType = "ResourceSpecification",
                TotalResources = 0,
                Resources = new List<System.Text.Json.Nodes.JsonNode>(),
                Validation = new DevProject.Data.Entities.ValidationReport { IsValid = true },
                Warnings = new List<string>()
            };

        private static readonly JsonSerializerOptions CamelCaseOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private JsonElement SerialiseResponse(IActionResult result)
        {
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value, CamelCaseOptions);
            return JsonDocument.Parse(json).RootElement;
        }

        [Fact]
        public async Task ConvertReturnsOkWithValidFileId()
        {
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(EmptyParsedWorkbook());

            automocker.GetMock<IJsonConversionProcessor>()
                .Setup(x => x.Convert(It.IsAny<ParsedExcelData>(), It.IsAny<JsonConversionConfig>()))
                .Returns(EmptyConversionResult());

            var sut = CreateTestSubject();
            var root = SerialiseResponse(await sut.Convert(new JsonConversionRequest { FileId = ExistingFileId }));

            Assert.Equal(ExistingFileId, root.GetProperty("fileId").GetString());
            Assert.Equal("Sheet1", root.GetProperty("sheetName").GetString());
        }

        [Fact]
        public async Task ConvertReturnsNotFoundWhenFileDoesNotExist()
        {
            var sut = CreateTestSubject();
            var result = await sut.Convert(new JsonConversionRequest { FileId = "nonexistent.xlsx" });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ConvertReturnsUnprocessableEntityWhenProcessingFails()
        {
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Throws(new ProcessExcelException("Parse error"));

            var sut = CreateTestSubject();
            var result = await sut.Convert(new JsonConversionRequest { FileId = ExistingFileId });

            var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var json = JsonSerializer.Serialize(unprocessable.Value);
            var root = JsonDocument.Parse(json).RootElement;
            Assert.Equal("Parse error", root.GetProperty("message").GetString());
        }

        [Fact]
        public async Task ConvertUsesDefaultConfigWhenNoneProvided()
        {
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(EmptyParsedWorkbook());

            JsonConversionConfig? capturedConfig = null;
            automocker.GetMock<IJsonConversionProcessor>()
                .Setup(x => x.Convert(It.IsAny<ParsedExcelData>(), It.IsAny<JsonConversionConfig>()))
                .Callback<ParsedExcelData, JsonConversionConfig>((_, c) => capturedConfig = c)
                .Returns(EmptyConversionResult());

            var sut = CreateTestSubject();
            await sut.Convert(new JsonConversionRequest { FileId = ExistingFileId });

            Assert.NotNull(capturedConfig);
            Assert.Equal("TMF634", capturedConfig!.ApiId);
            Assert.Null(capturedConfig.NameColumn);
        }

        [Fact]
        public async Task ConvertReturnsBadRequestOnUnexpectedException()
        {
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Throws(new Exception("Unexpected error"));

            var sut = CreateTestSubject();
            var result = await sut.Convert(new JsonConversionRequest { FileId = ExistingFileId });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unexpected error", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task ConvertSelectsCorrectSheetByName()
        {
            var workbook = new ParsedWorkbook
            {
                WorkbookName = "test",
                Sheets = new List<ParsedExcelData>
                {
                    new ParsedExcelData { SpreadsheetName = "Alpha", ColumnNames = new List<string> { "A" }, Rows = new List<Dictionary<string, CellValue>>() },
                    new ParsedExcelData { SpreadsheetName = "Beta", ColumnNames = new List<string> { "B" }, Rows = new List<Dictionary<string, CellValue>>() }
                }
            };

            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(workbook);

            ParsedExcelData? capturedData = null;
            automocker.GetMock<IJsonConversionProcessor>()
                .Setup(x => x.Convert(It.IsAny<ParsedExcelData>(), It.IsAny<JsonConversionConfig>()))
                .Callback<ParsedExcelData, JsonConversionConfig>((d, _) => capturedData = d)
                .Returns(EmptyConversionResult());

            var sut = CreateTestSubject();
            await sut.Convert(new JsonConversionRequest
            {
                FileId = ExistingFileId,
                Config = new JsonConversionConfig { ApiId = "TMF634", SheetName = "Beta" }
            });

            Assert.NotNull(capturedData);
            Assert.Equal("Beta", capturedData!.SpreadsheetName);
        }

        [Fact]
        public async Task ConvertReturnsUnprocessableEntityWhenNamedSheetNotFound()
        {
            var workbook = new ParsedWorkbook
            {
                WorkbookName = "test",
                Sheets = new List<ParsedExcelData>
                {
                    new ParsedExcelData { SpreadsheetName = "Alpha", ColumnNames = new List<string>(), Rows = new List<Dictionary<string, CellValue>>() }
                }
            };

            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(workbook);

            var sut = CreateTestSubject();
            var result = await sut.Convert(new JsonConversionRequest
            {
                FileId = ExistingFileId,
                Config = new JsonConversionConfig { ApiId = "TMF634", SheetName = "NonExistent" }
            });

            Assert.IsType<UnprocessableEntityObjectResult>(result);
        }

        [Fact]
        public void GetApiTypesReturnsOkWithCatalogList()
        {
            var catalog = new List<TmForumApiDefinition>
            {
                new TmForumApiDefinition
                {
                    ApiId = "TMF634",
                    Name = "Resource Catalog Management",
                    ResourceType = "ResourceSpecification",
                    BasePath = "/resourceCatalogManagement/v4",
                    ResourceCollectionPath = "resourceSpecification",
                    Version = "4.0"
                }
            };

            automocker.GetMock<ITmForumApiCatalogGetter>()
                .Setup(x => x.GetAll())
                .Returns(catalog);

            var sut = CreateTestSubject();
            var result = sut.GetApiTypes();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(catalog, ok.Value);
        }

        [Fact]
        public async Task DetectApiReturnsApiDetectionResultWithCandidates()
        {
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(EmptyParsedWorkbook());

            var detection = new DevProject.Data.Entities.ApiDetectionResult
            {
                Candidates = new List<DevProject.Data.Entities.ApiDetectionCandidate>
                {
                    new()
                    {
                        ApiId = "TMF622",
                        ApiName = "Product Ordering Management",
                        Confidence = 85,
                        MatchedSignals = new List<string> { "sheet:overview" },
                        MissingSignals = new List<string>(),
                    }
                },
                AutoSelectedApiId = "TMF622",
            };

            automocker.GetMock<ITmForumApiDetectorGetter>()
                .Setup(x => x.DetectApi(It.IsAny<DevProject.Data.Entities.ParsedWorkbook>()))
                .Returns(detection);

            var sut = CreateTestSubject();
            var root = SerialiseResponse(await sut.DetectApi(ExistingFileId));

            Assert.Equal("TMF622", root.GetProperty("autoSelectedApiId").GetString());
            Assert.Equal(1, root.GetProperty("candidates").GetArrayLength());
            Assert.Equal("TMF622", root.GetProperty("candidates")[0].GetProperty("apiId").GetString());
            Assert.Equal(85, root.GetProperty("candidates")[0].GetProperty("confidence").GetInt32());
        }

        [Fact]
        public async Task DetectApiReturnsNullAutoSelectedWhenNoCandidatePassesThreshold()
        {
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(EmptyParsedWorkbook());

            var detection = new DevProject.Data.Entities.ApiDetectionResult
            {
                Candidates = new List<DevProject.Data.Entities.ApiDetectionCandidate>(),
                AutoSelectedApiId = null,
            };

            automocker.GetMock<ITmForumApiDetectorGetter>()
                .Setup(x => x.DetectApi(It.IsAny<DevProject.Data.Entities.ParsedWorkbook>()))
                .Returns(detection);

            var sut = CreateTestSubject();
            var root = SerialiseResponse(await sut.DetectApi(ExistingFileId));

            Assert.True(root.GetProperty("autoSelectedApiId").ValueKind == System.Text.Json.JsonValueKind.Null);
        }

        [Fact]
        public async Task DetectApiReturnsNotFoundWhenFileDoesNotExist()
        {
            var sut = CreateTestSubject();
            var result = await sut.DetectApi("nonexistent.xlsx");

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
