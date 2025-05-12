using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using ScryfallApi.Client;
using StrongInject;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker;

[Register(typeof(MainViewModel), Scope.InstancePerResolution)]
[Register(typeof(CardsViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckCollectionViewModel), Scope.InstancePerResolution)]
[Register(typeof(ContainerSetViewModel), Scope.InstancePerResolution)]
[Register(typeof(WishlistViewModel), Scope.InstancePerResolution)]
[Register(typeof(ContainerBrowseViewModel), Scope.InstancePerResolution)]
[Register(typeof(CardSkuItemViewModel), Scope.InstancePerResolution)]
[Register(typeof(WishlistItemViewModel), Scope.InstancePerResolution)]
[Register(typeof(NotesViewModel), Scope.InstancePerResolution)]
[Register(typeof(CanIBuildThisDeckViewModel), Scope.InstancePerResolution)]
[Register(typeof(SettingsViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckDetailsViewModel), Scope.InstancePerResolution)]
[Register(typeof(EditCardSkuViewModel), Scope.InstancePerResolution)]
[Register(typeof(SplitCardSkuViewModel), Scope.InstancePerResolution)]
[Register(typeof(ContainerViewModel), Scope.InstancePerResolution)]
[Register(typeof(ContainerTextViewModel), Scope.InstancePerResolution)]
[Register(typeof(DialogViewModel), Scope.InstancePerResolution)]
[Register(typeof(AddCardsViewModel), Scope.InstancePerResolution)]
[Register(typeof(AddCardsToWishlistViewModel), Scope.InstancePerResolution)]
[Register(typeof(SendCardsToContainerOrDeckViewModel), Scope.InstancePerResolution)]
[Register(typeof(NewDeckOrContainerViewModel), Scope.InstancePerResolution)]
[Register(typeof(EditDeckOrContainerViewModel), Scope.InstancePerResolution)]
[Register(typeof(ManageVendorsViewModel), Scope.InstancePerResolution)]
[Register(typeof(EditWishlistItemViewModel), Scope.InstancePerResolution)]
[Register(typeof(MoveWishlistItemsToCollectionViewModel), Scope.InstancePerResolution)]
[Register(typeof(CardsDbContext), Scope.InstancePerResolution)]
[Register(typeof(CollectionTrackingService), Scope.InstancePerResolution, typeof(ICollectionTrackingService))]
#pragma warning disable SI1103 // Return type of delegate has a single instance scope and so will always have the same value
public partial class Container : IContainer<MainViewModel>
#pragma warning restore SI1103 // Return type of delegate has a single instance scope and so will always have the same value
{
    readonly Visual _root;

    public Container(Visual root)
    {
        _root = root;
    }

    [Factory]
    public DbContextOptions<CardsDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<CardsDbContext>()
            .UseSqlite("Data Source=collection.sqlite")
            .Options;
    }

    [Factory(Scope.SingleInstance)]
    public IMessenger GetMessenger() => WeakReferenceMessenger.Default;

    [Factory(Scope.SingleInstance)]
    public IStorageProvider GetStorageProvider() => TopLevel.GetTopLevel(_root).StorageProvider;

    class ScryfallHttpHandler : DelegatingHandler
    {
        readonly Random _rnd = new();

        public ScryfallHttpHandler()
            : base(new HttpClientHandler())
        { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            /*
            From their API docs: 
            
            We kindly ask that you insert 50 – 100 milliseconds of delay between the requests you send to 
            the server at api.scryfall.com. (i.e., 10 requests per second on average).
             */
            var delayMs = _rnd.Next(50, 100);
            System.Diagnostics.Debug.WriteLine($"Adding {delayMs}ms delay to scryfall request");
            await Task.Delay(delayMs, cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }

    [Factory(Scope.SingleInstance)]
    public IScryfallApiClient CreateScryfallClient()
    {
        var http = new HttpClient(new ScryfallHttpHandler())
        {
            BaseAddress = new System.Uri("https://api.scryfall.com/")
        };
        http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        return new ScryfallClient(http);
    }
}
