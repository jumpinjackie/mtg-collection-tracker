using ScryfallApi.Client;
using ScryfallApi.Client.Apis;

namespace MtgCollectionTracker.Core.Services;

public class ScryfallClient : IScryfallApiClient
{
    readonly IScryfallApiClient _inner;
    readonly HttpClient _innerHttp;

    public ScryfallClient(HttpClient inner)
    {
        _inner = new ScryfallApiClient(inner);
        _innerHttp = inner;
    }

    public HttpClient RawClient => _innerHttp;

    public ICards Cards => _inner.Cards;

    public ICatalogs Catalogs => _inner.Catalogs;

    public ISets Sets => _inner.Sets;

    public ISymbology Symbology => _inner.Symbology;
}
