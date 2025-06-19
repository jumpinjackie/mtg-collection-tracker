using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Data;
using ScryfallApi.Client;
using StrongInject;
using System.Linq.Expressions;

namespace MtgCollectionTracker.Core.Services;

public class CardImageCache(Func<Owned<CardsDbContext>> _db, IScryfallApiClient client)
{
    private async ValueTask<Stream?> GetCardFaceImageAsync(string scryfallId, string tag, Expression<Func<ScryfallCardMetadata, string?>> urlSelector)
    {
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
                var stream = await resp.Content.ReadAsStreamAsync();
                return stream;
            }

            //var stream = MemoryStreamPool.GetStream(tag, imgUrl);
            //return stream;
        }
        return null;
    }

    public async ValueTask<Stream?> GetLargeFrontFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_front_face_large", m => m.ImageLargeUrl);
    }

    public async ValueTask<Stream?> GetLargeBackFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_back_face_large", m => m.BackImageLargeUrl);
    }

    public async ValueTask<Stream?> GetSmallFrontFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_front_face_small", m => m.ImageSmallUrl);
    }

    public async ValueTask<Stream?> GetSmallBackFaceImageAsync(string scryfallId)
    {
        return await GetCardFaceImageAsync(scryfallId, "img_back_face_small", m => m.BackImageSmallUrl);
    }
}
