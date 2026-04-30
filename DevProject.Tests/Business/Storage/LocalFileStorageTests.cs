namespace DevProject.Tests.Business.Storage
{
    using DevProject.Business.Storage;
    using Microsoft.AspNetCore.Hosting;
    using Moq;

    public class LocalFileStorageTests : TestBase<LocalFileStorage>, IDisposable
    {
        private readonly string tempRoot;

        public LocalFileStorageTests()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempRoot);

            automocker.GetMock<IWebHostEnvironment>()
                .Setup(x => x.WebRootPath)
                .Returns(tempRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }

        [Fact]
        public async Task SaveAsyncCreatesFileAndReturnsFileId()
        {
            var content = System.Text.Encoding.UTF8.GetBytes("fake excel bytes");
            using var stream = new MemoryStream(content);

            var sut = CreateTestSubject();
            var fileId = await sut.SaveAsync(stream, ".xlsx", CancellationToken.None);

            Assert.EndsWith(".xlsx", fileId);
            Assert.True(File.Exists(Path.Combine(tempRoot, "uploads", fileId)));
        }

        [Fact]
        public async Task SaveAsyncWritesCorrectContent()
        {
            var content = new byte[] { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(content);

            var sut = CreateTestSubject();
            var fileId = await sut.SaveAsync(stream, ".xlsx", CancellationToken.None);

            var written = await File.ReadAllBytesAsync(Path.Combine(tempRoot, "uploads", fileId));
            Assert.Equal(content, written);
        }

        [Fact]
        public async Task SaveAsyncCreatesUploadsDirectoryIfMissing()
        {
            var uploadsDir = Path.Combine(tempRoot, "uploads");
            Assert.False(Directory.Exists(uploadsDir));

            using var stream = new MemoryStream(new byte[] { 0 });
            var sut = CreateTestSubject();
            await sut.SaveAsync(stream, ".xlsx", CancellationToken.None);

            Assert.True(Directory.Exists(uploadsDir));
        }

        [Fact]
        public async Task ExistsAsyncReturnsTrueForSavedFile()
        {
            using var stream = new MemoryStream(new byte[] { 0 });
            var sut = CreateTestSubject();
            var fileId = await sut.SaveAsync(stream, ".xlsx", CancellationToken.None);

            var exists = await sut.ExistsAsync(fileId, CancellationToken.None);

            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsyncReturnsFalseForMissingFile()
        {
            var sut = CreateTestSubject();

            var exists = await sut.ExistsAsync("nonexistent.xlsx", CancellationToken.None);

            Assert.False(exists);
        }

        [Fact]
        public async Task OpenReadAsyncReturnsSavedContentAsSeekableStream()
        {
            var content = new byte[] { 10, 20, 30 };
            using var input = new MemoryStream(content);
            var sut = CreateTestSubject();
            var fileId = await sut.SaveAsync(input, ".xlsx", CancellationToken.None);

            using var result = await sut.OpenReadAsync(fileId, CancellationToken.None);

            Assert.True(result.CanSeek);
            Assert.Equal(0, result.Position);
            var read = new byte[result.Length];
            await result.ReadAsync(read);
            Assert.Equal(content, read);
        }

        [Fact]
        public async Task DeleteAsyncRemovesFile()
        {
            using var stream = new MemoryStream(new byte[] { 0 });
            var sut = CreateTestSubject();
            var fileId = await sut.SaveAsync(stream, ".xlsx", CancellationToken.None);
            Assert.True(await sut.ExistsAsync(fileId, CancellationToken.None));

            await sut.DeleteAsync(fileId, CancellationToken.None);

            Assert.False(await sut.ExistsAsync(fileId, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsyncDoesNotThrowWhenFileDoesNotExist()
        {
            var sut = CreateTestSubject();

            var ex = await Record.ExceptionAsync(
                () => sut.DeleteAsync("nonexistent.xlsx", CancellationToken.None));

            Assert.Null(ex);
        }

        [Fact]
        public async Task SaveAsyncGeneratesUniqueFileIds()
        {
            var sut = CreateTestSubject();

            var id1 = await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), ".xlsx", CancellationToken.None);
            var id2 = await sut.SaveAsync(new MemoryStream(new byte[] { 2 }), ".xlsx", CancellationToken.None);

            Assert.NotEqual(id1, id2);
        }
    }
}
