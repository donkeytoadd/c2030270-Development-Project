namespace DevProject.Tests.Controllers
{
    using DevProject.Controllers;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    public class FileUploadControllerTests : TestBase<FileUploadController>
    { 
        private Mock<IFormFile> CreateMockFile(string fileName, int fileSize, string content) 
        { 
            var mockFile = new Mock<IFormFile>();
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(fileSize);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
    
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mockFile;
        }

        [Theory]
        [InlineData("document.xlsx")]
        [InlineData("test.xls")]
        public async Task UploadFileReturnsOkWithValidInput(string fileName)
        {
            var mockFile = this.CreateMockFile(fileName, 1024, "mock content");

            var sut = this.CreateTestSubject();
            
            var result = await sut.UploadFile(mockFile.Object);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedFileName = Assert.IsType<string>(okResult.Value);
            Assert.EndsWith(Path.GetExtension(fileName), returnedFileName);
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestWithEmptyFile()
        {
            var mockFile = this.CreateMockFile("file.xls", 0, "mock content");
            
            var sut = this.CreateTestSubject();
            
            var result = await sut.UploadFile(mockFile.Object);
            
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No File Selected", badRequest.Value);
        }

        [Theory]
        [InlineData("document.pdf")]
        [InlineData("test.docx")]
        [InlineData("image.png")]
        public async Task UploadFileReturnsBadRequestWithInvalidFileType(string fileName)
        {
            var mockFile = this.CreateMockFile(fileName, 1024, "mock content");

            var sut = this.CreateTestSubject();
            
            var result = await sut.UploadFile(mockFile.Object);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UploadFileReturnsBadRequestWithLargeFileSize()
        {
            var mockFile = this.CreateMockFile("document.xlsx", 11 * 1024 * 1024,  "mock content");

            var sut = this.CreateTestSubject();
            
            var result = await sut.UploadFile(mockFile.Object);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("File size cannot be larger than", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task UploadFileThrowsException()
        {
            var mockFile = this.automocker.GetMock<IFormFile>();
            
            mockFile.Setup(x => x.FileName).Returns("file.xlsx");
            mockFile.Setup(x => x.Length).Returns(10 * 1024 * 1024);
            mockFile.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test Exception"));
            
            var sut = this.CreateTestSubject();
            
            var result = await sut.UploadFile(mockFile.Object);
            
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Test Exception", badRequest.Value!.ToString());
        }
    }
}