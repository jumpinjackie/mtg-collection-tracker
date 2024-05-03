using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
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

[Register(typeof(MainViewModel), Scope.SingleInstance)]
[Register(typeof(CardsViewModel), Scope.SingleInstance)]
[Register(typeof(DeckCollectionViewModel), Scope.SingleInstance)]
[Register(typeof(ContainerSetViewModel), Scope.SingleInstance)]
[Register(typeof(ContainerBrowseViewModel), Scope.InstancePerResolution)]
[Register(typeof(CardSkuItemViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckListViewModel), Scope.InstancePerResolution)]
[Register(typeof(EditCardSkuViewModel), Scope.InstancePerResolution)]
[Register(typeof(SplitCardSkuViewModel), Scope.InstancePerResolution)]
[Register(typeof(ContainerViewModel), Scope.InstancePerResolution)]
[Register(typeof(DrawerViewModel), Scope.InstancePerResolution)]
[Register(typeof(AddCardsViewModel), Scope.InstancePerResolution)]
[Register(typeof(SendCardsToContainerOrDeckViewModel), Scope.InstancePerResolution)]
[Register(typeof(CardsDbContext), Scope.InstancePerResolution)]
[Register(typeof(CollectionTrackingService), Scope.InstancePerResolution, typeof(ICollectionTrackingService))]
[Register(typeof(ViewModelFactory), Scope.SingleInstance, typeof(IViewModelFactory))]
#pragma warning disable SI1103 // Return type of delegate has a single instance scope and so will always have the same value
public partial class Container : IContainer<MainViewModel>
#pragma warning restore SI1103 // Return type of delegate has a single instance scope and so will always have the same value
{
    [Factory]
    public DbContextOptions<CardsDbContext> CreateDbContextOptions()
    {
        return new DbContextOptionsBuilder<CardsDbContext>()
            .UseSqlite("Data Source=collection.sqlite")
            .Options;
    }

    [Factory(Scope.SingleInstance)]
    public IMessenger GetMessenger() => WeakReferenceMessenger.Default;

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
        return new ScryfallApiClient(http);
    }
}
