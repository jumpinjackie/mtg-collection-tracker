using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using StrongInject;
using System.Linq.Expressions;

namespace MtgCollectionTracker.Core.Services;

public class CardImageCache(Func<Owned<CardsDbContext>> _db, ICardImageFileSystem fs, IScryfallApiClient client)
{
    private async ValueTask<Stream?> GetCardFaceImageAsync(string scryfallId, string tag, Expression<Func<ScryfallCardMetadata, string?>> urlSelector)
    {
        var cachedStream = fs.TryGetStream(scryfallId, tag);
        if (cachedStream != null)
            return cachedStream;

        using var db = _db.Invoke();
        var imgUrl = await db.Value.Set<ScryfallCardMetadata>()
            .Where(m => m.Id == scryfallId)
            .Select(urlSelector)
            .FirstOrDefaultAsync();
        if (imgUrl != null && client is ScryfallClient sc)
        {
            var resp = await sc.RawClient.GetAsync(imgUrl);
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Write to disk then immediately close the file to avoid holding an
                // exclusive lock that would block concurrent readers of the same image.
                using (var writeStream = fs.OpenStream(scryfallId, tag))
                {
                    await resp.Content.CopyToAsync(writeStream);
                }
                // Re-read into a MemoryStream so callers never hold a FileStream open.
                return fs.TryGetStream(scryfallId, tag);
            }
        }
        return null;
    }

    /// <summary>
    /// Fetches the back-face image for a card, but only when the back-face URL is distinct from
    /// the front-face URL.  Adventure cards (e.g. "Questing Druid // Seek the Beast") may have
    /// <paramref name="backUrlSelector"/> populated with the same URL as the front face because
    /// both halves share the same physical card image.  Returning null for those cards prevents
    /// them from being rendered as true double-faced cards.
    /// </summary>
    private async ValueTask<Stream?> GetBackFaceImageAsync(
        string scryfallId,
        string tag,
        Expression<Func<ScryfallCardMetadata, string?>> frontUrlSelector,
        Expression<Func<ScryfallCardMetadata, string?>> backUrlSelector)
    {
        // Check file cache first (most common path after the first fetch).
        var cachedStream = fs.TryGetStream(scryfallId, tag);
        if (cachedStream != null)
            return cachedStream;

        // Query both face URLs to detect adventure cards whose back-face URL equals the
        // front-face URL (both halves share the same physical card image).
        using var db = _db.Invoke();
        var frontUrl = await db.Value.Set<ScryfallCardMetadata>()
            .Where(m => m.Id == scryfallId)
            .Select(frontUrlSelector)
            .FirstOrDefaultAsync();
        var backUrl = await db.Value.Set<ScryfallCardMetadata>()
            .Where(m => m.Id == scryfallId)
            .Select(backUrlSelector)
            .FirstOrDefaultAsync();

        // No back-face URL, or it equals the front-face URL → not a true DFC.
        if (backUrl == null || backUrl == frontUrl)
            return null;

        if (client is ScryfallClient sc)
        {
            var resp = await sc.RawClient.GetAsync(backUrl);
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                using (var writeStream = fs.OpenStream(scryfallId, tag))
                {
                    await resp.Content.CopyToAsync(writeStream);
                }
                return fs.TryGetStream(scryfallId, tag);
            }
        }
        return null;
    }

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_front_face_large", m => m.ImageLargeUrl);
    }

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_front_face_small", m => m.ImageSmallUrl);
    }

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId)
    {
        return await GetBackFaceImageAsync(scryfallId, "img_back_face_small", m => m.ImageSmallUrl, m => m.BackImageSmallUrl);
    }

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId)
    {
        return await GetBackFaceImageAsync(scryfallId, "img_back_face_large", m => m.ImageLargeUrl, m => m.BackImageLargeUrl);
    }
}
