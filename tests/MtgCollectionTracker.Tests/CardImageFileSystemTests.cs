using Microsoft.IO;
using MtgCollectionTracker.Core.Services;
using System;
using System.IO;
using System.Text;

namespace MtgCollectionTracker.Tests;

public class CardImageFileSystemTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"mtg-image-fs-tests-{Guid.NewGuid():N}");

    public CardImageFileSystemTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void TryGetStream_ImageExists_ReturnsRecyclableMemoryStream()
    {
        var sut = new CardImageFileSystem(_tempDir);
        var filePath = Path.Combine(_tempDir, "card-id_img_front_face_small.jpg");
        var expected = Encoding.UTF8.GetBytes("image-bytes");
        File.WriteAllBytes(filePath, expected);

        using var stream = sut.TryGetStream("card-id", "img_front_face_small");

        Assert.NotNull(stream);
        Assert.IsType<RecyclableMemoryStream>(stream);
        Assert.Equal(expected, ReadAllBytes(stream));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        stream.Position = 0;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
