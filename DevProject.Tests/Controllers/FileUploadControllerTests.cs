namespace DevProject.Tests.Controllers
{
    using DevProject.Business.Processors.Interfaces;
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
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(fileSize);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockRequest = new FileUploadRequest()
            {
                File = mockFile.Object
            };

            return mockRequest;
        }

        [Fact]
        public async Task UploadFileReturnsOkWithParsedData()
        {
            var parsedData = new ParsedExcelData
            {
                SpreadsheetName = "Products",
                ColumnNames = new List<string>
                {
                    "Id", 
                    "Name"
                },
                Rows = new List<Dictionary<string, CellValue>>(),
                TotalRows = 0
            };

            this.automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(parsedData);

            var mockFile = this.CreateMockFile("products.xlsx", 1024, "mock content");
            
            var sut = this.CreateTestSubject();

            var result = await sut.UploadFile(mockFile);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json= System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            var root = System.Text.Json.JsonDocument.Parse(json).RootElement;

            Assert.Equal("Products", root.GetProperty("SheetName").GetString());
            Assert.EndsWith(".xlsx", root.GetProperty("FileId").GetString());
            Assert.Equal(2,      root.GetProperty("ColumnNames").GetArrayLength());
            Assert.Equal("Id",   root.GetProperty("ColumnNames")[0].GetString());
            Assert.Equal("Name", root.GetProperty("ColumnNames")[1].GetString());
            Assert.Equal(0,      root.GetProperty("Rows").GetArrayLength());
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestForEmptyFile()
        {
            var mockFile = this.CreateMockFile("file.xlsx", 0, "mock");
            
            var sut = this.CreateTestSubject();

            var result = await sut.UploadFile(mockFile);
            
            var badRequest  = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No File Selected", badRequest.Value);
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestForOversizedFile()
        {
            var mockFile = this.CreateMockFile("file.xlsx", 11 * 1024 * 1024, "mock");
            
            var sut = this.CreateTestSubject();
            
            var result = await sut.UploadFile(mockFile);
            var badRequest  = Assert.IsType<BadRequestObjectResult>(result);
            
            Assert.Contains("File size cannot be larger than", badRequest.Value!.ToString());
        }

        [Theory]
        [InlineData("document.pdf")]
        [InlineData("test.docx")]
        [InlineData("image.png")]
        public async Task UploadFileReturnsBadRequestWithInvalidFileExtension(string fileName)
        {
            var mockFile = this.CreateMockFile(fileName, 1024, "mock content");

            var sut = this.CreateTestSubject();

            var result = await sut.UploadFile(mockFile);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadFileReturnsUnprocessableEntityWhenProcessingFails()
        {
            this.automocker.GetMock<ISpreadsheetProcessor>()
                .Setup(x => x.Process(It.IsAny<Stream>(), It.IsAny<string>()))
                .Throws(new ProcessExcelException("Invalid workbook structure"));

            var mockFile = this.CreateMockFile("file.xlsx", 1024, "mock content");

            var sut = this.CreateTestSubject();

            var result = await sut.UploadFile(mockFile);

            var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var json = System.Text.Json.JsonSerializer.Serialize(unprocessable.Value);
            var root = System.Text.Json.JsonDocument.Parse(json).RootElement;

            Assert.Equal("Invalid workbook structure", root.GetProperty("message").GetString());
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestWhenUnexpectedExceptionThrown()
        {
            var mockFile = this.automocker.GetMock<IFormFile>();

            mockFile.Setup(x => x.FileName).Returns("file.xlsx");
            mockFile.Setup(x => x.Length).Returns(1024);
            mockFile.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var mockRequest = new FileUploadRequest()
            {
                File = mockFile.Object
            };

            var sut = this.CreateTestSubject();

            var result = await sut.UploadFile(mockRequest);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unexpected error", badRequest.Value!.ToString());
        }
    }
}