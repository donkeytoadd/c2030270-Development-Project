namespace DevProject.Tests.Controllers
{
    using DevProject.Business.Processors.Interfaces;
    using DevProject.Business.Storage.Interfaces;
    using DevProject.Controllers;
    using DevProject.Data.Entities;
    using DevProject.Data.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using Resources.Requests;

    public class FileUploadControllerTests : TestBase<FileUploadController>
    {
        private FileUploadRequest CreateMockFile(string fileName, long fileSize, string content)
        {
            var mockFile = new Mock<IFormFile>();
            var stream   = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(fileSize);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            return new FileUploadRequest { File = mockFile.Object };
        }

        private static ParsedWorkbook BuildWorkbook(string workbookName, params ParsedExcelData[] sheets) =>
            new ParsedWorkbook
            {
                WorkbookName = workbookName,
                Sheets       = new List<ParsedExcelData>(sheets)
            };

        private void SetupFileStorageSave(string returnedFileId = "fake-id.xlsx")
        {
            automocker.GetMock<IFileStorage>()
                .Setup(x => x.SaveAsync(
                    It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedFileId);
        }

        [Fact]
        public async Task UploadFileReturnsOkWithParsedData()
        {
            var workbook = BuildWorkbook("products",
                new ParsedExcelData
                {
                    SpreadsheetName = "Products",
                    ColumnNames     = new List<string> { "Id", "Name" },
                    Rows            = new List<Dictionary<string, CellValue>>(),
                    TotalRows       = 0
                });

            SetupFileStorageSave("fake-id.xlsx");
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(workbook);

            var mockFile = CreateMockFile("products.xlsx", 1024, "mock content");

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockFile);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json     = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var root     = System.Text.Json.JsonDocument.Parse(json).RootElement;

            Assert.Equal("products", root.GetProperty("WorkbookName").GetString());
            Assert.EndsWith(".xlsx", root.GetProperty("FileId").GetString());
            Assert.Equal(1, root.GetProperty("TotalSheets").GetInt32());

            var firstSheet = root.GetProperty("Sheets")[0];
            Assert.Equal("Products", firstSheet.GetProperty("SheetName").GetString());
            Assert.Equal(2, firstSheet.GetProperty("ColumnNames").GetArrayLength());
            Assert.Equal("Id",   firstSheet.GetProperty("ColumnNames")[0].GetString());
            Assert.Equal("Name", firstSheet.GetProperty("ColumnNames")[1].GetString());
            Assert.Equal(0, firstSheet.GetProperty("TotalRows").GetInt32());
        }

        [Fact]
        public async Task UploadFileReturnsOkWithMultipleSheets()
        {
            var workbook = BuildWorkbook("catalogue",
                new ParsedExcelData
                {
                    SpreadsheetName = "Service",
                    ColumnNames     = new List<string> { "name", "description" },
                    Rows            = new List<Dictionary<string, CellValue>>(),
                    TotalRows       = 0
                },
                new ParsedExcelData
                {
                    SpreadsheetName = "RelatedParty",
                    ColumnNames     = new List<string> { "id", "role" },
                    Rows            = new List<Dictionary<string, CellValue>>(),
                    TotalRows       = 0
                });

            SetupFileStorageSave("fake-id.xlsx");
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(workbook);

            var mockFile = CreateMockFile("catalogue.xlsx", 2048, "mock content");

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockFile);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json     = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var root     = System.Text.Json.JsonDocument.Parse(json).RootElement;

            Assert.Equal(2, root.GetProperty("TotalSheets").GetInt32());
            Assert.Equal("Service",      root.GetProperty("Sheets")[0].GetProperty("SheetName").GetString());
            Assert.Equal("RelatedParty", root.GetProperty("Sheets")[1].GetProperty("SheetName").GetString());
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestForEmptyFile()
        {
            var mockFile = CreateMockFile("file.xlsx", 0, "mock");

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockFile);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No File Selected", badRequest.Value);
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestForOversizedFile()
        {
            var mockFile = CreateMockFile("file.xlsx", 11 * 1024 * 1024, "mock");

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockFile);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("File size cannot be larger than", badRequest.Value!.ToString());
        }

        [Theory]
        [InlineData("document.pdf")]
        [InlineData("test.docx")]
        [InlineData("image.png")]
        public async Task UploadFileReturnsBadRequestWithInvalidFileExtension(string fileName)
        {
            var mockFile = CreateMockFile(fileName, 1024, "mock content");

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockFile);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadFileReturnsUnprocessableEntityWhenProcessingFails()
        {
            SetupFileStorageSave();
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Throws(new ProcessExcelException("Invalid workbook structure"));

            var mockFile = CreateMockFile("file.xlsx", 1024, "mock content");

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockFile);

            var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(unprocessable.Value);
            var root = System.Text.Json.JsonDocument.Parse(json).RootElement;

            Assert.Equal("Invalid workbook structure", root.GetProperty("message").GetString());
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestWhenUnexpectedExceptionThrown()
        {
            var mockFile = automocker.GetMock<IFormFile>();

            mockFile.Setup(x => x.FileName).Returns("file.xlsx");
            mockFile.Setup(x => x.Length).Returns(1024);
            mockFile.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Unexpected error"));

            var mockRequest = new FileUploadRequest { File = mockFile.Object };

            var sut    = CreateTestSubject();
            var result = await sut.UploadFile(mockRequest);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unexpected error", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task UploadFileDoesNotTouchFilesystemDirectly()
        {
            SetupFileStorageSave("saved-file.xlsx");
            automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(BuildWorkbook("wb", new ParsedExcelData
                {
                    SpreadsheetName = "Sheet1",
                    ColumnNames     = new List<string>(),
                    Rows            = new List<Dictionary<string, CellValue>>()
                }));

            var mockFile = CreateMockFile("test.xlsx", 512, "content");
            var sut      = CreateTestSubject();
            await sut.UploadFile(mockFile);

            automocker.GetMock<IFileStorage>()
                .Verify(x => x.SaveAsync(
                    It.IsAny<Stream>(), ".xlsx", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}