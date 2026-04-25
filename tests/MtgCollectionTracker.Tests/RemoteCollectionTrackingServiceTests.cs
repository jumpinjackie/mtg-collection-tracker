using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IO;
using MtgCollectionTracker.ApiClient;

namespace MtgCollectionTracker.Tests;

public class RemoteCollectionTrackingServiceTests
{
    [Fact]
    public async Task GetSmallFrontFaceImageAsync_ServerReturnsImage_ReturnsSeekableStream()
    {
        // Arrange
        var imageBytes = Encoding.UTF8.GetBytes("fake-image");
        using var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new NonSeekableReadStream(imageBytes))
        });
        var service = new RemoteCollectionTrackingService(httpClient);

        // Act
        using var stream = await service.GetSmallFrontFaceImageAsync("12345678-1234-1234-1234-123456789abc");

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.CanSeek);
        Assert.IsType<RecyclableMemoryStream>(stream);
        Assert.Equal(imageBytes, ReadAllBytes(stream));
    }

    [Fact]
    public async Task GetLargeFrontFaceImageAsync_ServerReturnsNotFound_ReturnsNull()
    {
        // Arrange
        using var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = new RemoteCollectionTrackingService(httpClient);

        // Act
        var stream = await service.GetLargeFrontFaceImageAsync(Guid.NewGuid());

        // Assert
        Assert.Null(stream);
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost:5757")
        };
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        stream.Position = 0;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
