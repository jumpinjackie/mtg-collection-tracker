using MtgCollectionTracker.Core;
using ScryfallApi.Client.Models;

namespace MtgCollectionTracker.Tests;

/// <summary>
/// Tests for the internal <c>InternalExtensionMethods</c> class in
/// <c>MtgCollectionTracker.Core</c>, specifically <c>GetBackFaceImageUri</c>
/// and <c>GetFrontFaceImageUri</c> which are central to the adventure-card /
/// double-faced-card distinction.
/// </summary>
public class ExtensionMethodsTests
{
    private const string Size = "normal";

    private static readonly Uri FrontUri = new("https://cards.example.com/front.jpg");
    private static readonly Uri BackUri  = new("https://cards.example.com/back.jpg");
    private static readonly Uri RootUri  = new("https://cards.example.com/root.jpg");

    // ── GetBackFaceImageUri ──────────────────────────────────────────────────

    [Fact]
    public void GetBackFaceImageUri_ReturnsNull_ForSingleFaceCard_WithNoCardFaces()
    {
        var card = new Card { ImageUris = new() { { Size, RootUri } } };

        var result = card.GetBackFaceImageUri(Size);

        Assert.Null(result);
    }

    [Fact]
    public void GetBackFaceImageUri_ReturnsNull_ForAdventureCard_WhenFrontFaceHasNoImageUris()
    {
        // Adventure cards (e.g. "Questing Druid // Seek the Beast") share the root image_uris.
        // CardFaces[0].ImageUris is null for adventure cards — this is the reliable indicator.
        var card = new Card
        {
            ImageUris = new() { { Size, RootUri } },
            CardFaces = new[]
            {
                new CardFace { Name = "Questing Druid",    ImageUris = null },
                new CardFace { Name = "Seek the Beast",    ImageUris = null },
            }
        };

        var result = card.GetBackFaceImageUri(Size);

        Assert.Null(result);
    }

    [Fact]
    public void GetBackFaceImageUri_ReturnsBackFaceUri_ForTrueDFC_WhenBothFacesHaveDistinctImageUris()
    {
        // True double-faced cards (transform / modal DFC) have per-face image_uris on BOTH faces.
        var card = new Card
        {
            CardFaces = new[]
            {
                new CardFace { Name = "Delver of Secrets",   ImageUris = new() { { Size, FrontUri } } },
                new CardFace { Name = "Insectile Aberration", ImageUris = new() { { Size, BackUri  } } },
            }
        };

        var result = card.GetBackFaceImageUri(Size);

        Assert.Equal(BackUri, result);
    }

    [Fact]
    public void GetBackFaceImageUri_ReturnsNull_WhenRequestedSizeNotPresentOnBackFace()
    {
        var card = new Card
        {
            CardFaces = new[]
            {
                new CardFace { ImageUris = new() { { Size, FrontUri } } },
                new CardFace { ImageUris = new() { { "large", BackUri } } }, // only "large", not "normal"
            }
        };

        var result = card.GetBackFaceImageUri(Size);

        Assert.Null(result);
    }

    // ── GetFrontFaceImageUri ─────────────────────────────────────────────────

    [Fact]
    public void GetFrontFaceImageUri_ReturnsRootImageUri_ForSingleFaceCard()
    {
        var card = new Card { ImageUris = new() { { Size, RootUri } } };

        var result = card.GetFrontFaceImageUri(Size);

        Assert.Equal(RootUri, result);
    }

    [Fact]
    public void GetFrontFaceImageUri_ReturnsRootImageUri_ForAdventureCard_WhenFrontFaceHasNoImageUris()
    {
        // Adventure cards store the image on the root; CardFaces[0].ImageUris is null.
        var card = new Card
        {
            ImageUris = new() { { Size, RootUri } },
            CardFaces = new[]
            {
                new CardFace { Name = "Questing Druid",  ImageUris = null },
                new CardFace { Name = "Seek the Beast",  ImageUris = null },
            }
        };

        var result = card.GetFrontFaceImageUri(Size);

        Assert.Equal(RootUri, result);
    }

    [Fact]
    public void GetFrontFaceImageUri_ReturnsFrontFaceImageUri_ForTrueDFC()
    {
        var card = new Card
        {
            CardFaces = new[]
            {
                new CardFace { Name = "Delver of Secrets",    ImageUris = new() { { Size, FrontUri } } },
                new CardFace { Name = "Insectile Aberration", ImageUris = new() { { Size, BackUri  } } },
            }
        };

        var result = card.GetFrontFaceImageUri(Size);

        Assert.Equal(FrontUri, result);
    }
}
