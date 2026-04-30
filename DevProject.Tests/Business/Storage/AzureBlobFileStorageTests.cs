namespace DevProject.Tests.Business.Storage
{
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using DevProject.Business.Storage;
    using Moq;

    public class AzureBlobFileStorageTests : TestBase<AzureBlobFileStorage>
    {
        private readonly Mock<BlobContainerClient> containerMock;
        private readonly Mock<BlobClient> blobMock;

        public AzureBlobFileStorageTests()
        {
            containerMock = automocker.GetMock<BlobContainerClient>();
            blobMock = new Mock<BlobClient>();

            containerMock
                .Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(blobMock.Object);

            containerMock
                .Setup(x => x.CreateIfNotExistsAsync(
                    It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());
        }

        [Fact]
        public async Task SaveAsyncReturnsFileIdWithExtension()
        {
            blobMock
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var sut = CreateTestSubject();
            var fileId = await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), ".xlsx", CancellationToken.None);

            Assert.EndsWith(".xlsx", fileId);
        }

        [Fact]
        public async Task SaveAsyncCallsUploadOnBlobClient()
        {
            blobMock
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var sut = CreateTestSubject();
            await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), ".xlsx", CancellationToken.None);

            blobMock.Verify(x => x.UploadAsync(
                It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsyncEnsuresContainerExists()
        {
            blobMock
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var sut = CreateTestSubject();
            await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), ".xlsx", CancellationToken.None);

            containerMock.Verify(x => x.CreateIfNotExistsAsync(
                It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsyncOnlyEnsuresContainerOnce()
        {
            blobMock
                .Setup(x => x.UploadAsync(
                    It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            var sut = CreateTestSubject();
            await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), ".xlsx", CancellationToken.None);
            await sut.SaveAsync(new MemoryStream(new byte[] { 2 }), ".xlsx", CancellationToken.None);

            containerMock.Verify(x => x.CreateIfNotExistsAsync(
                It.IsAny<Azure.Storage.Blobs.Models.PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task OpenReadAsyncReturnsSeekableMemoryStream()
        {
            var content = new byte[] { 10, 20, 30 };

            blobMock
                .Setup(x => x.DownloadToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, _) => stream.Write(content))
                .ReturnsAsync(Mock.Of<Response>());

            var sut = CreateTestSubject();
            using var result = await sut.OpenReadAsync("some-file.xlsx", CancellationToken.None);

            Assert.True(result.CanSeek);
            Assert.Equal(0, result.Position);
            Assert.Equal(content.Length, result.Length);
        }

        [Fact]
        public async Task ExistsAsyncReturnsTrueWhenBlobExists()
        {
            var responseMock = new Mock<Response<bool>>();
            responseMock.SetupGet(x => x.Value).Returns(true);

            blobMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            var sut = CreateTestSubject();
            var exists = await sut.ExistsAsync("file.xlsx", CancellationToken.None);

            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsyncReturnsFalseWhenBlobDoesNotExist()
        {
            var responseMock = new Mock<Response<bool>>();
            responseMock.SetupGet(x => x.Value).Returns(false);

            blobMock
                .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseMock.Object);

            var sut = CreateTestSubject();
            var exists = await sut.ExistsAsync("missing.xlsx", CancellationToken.None);

            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteAsyncCallsDeleteIfExistsOnBlobClient()
        {
            blobMock
                .Setup(x => x.DeleteIfExistsAsync(
                    It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<Response<bool>>());

            var sut = CreateTestSubject();
            await sut.DeleteAsync("file.xlsx", CancellationToken.None);

            blobMock.Verify(x => x.DeleteIfExistsAsync(
                It.IsAny<Azure.Storage.Blobs.Models.DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
