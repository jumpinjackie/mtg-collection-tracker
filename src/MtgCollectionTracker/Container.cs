using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.ViewModels;
using StrongInject;

namespace MtgCollectionTracker;

[Register(typeof(MainViewModel), Scope.SingleInstance)]
[Register(typeof(CardsViewModel), Scope.SingleInstance)]
[Register(typeof(DeckCollectionViewModel), Scope.SingleInstance)]
[Register(typeof(ContainerSetViewModel), Scope.SingleInstance)]
[Register(typeof(CardSkuItemViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckViewModel), Scope.InstancePerResolution)]
[Register(typeof(DeckListViewModel), Scope.InstancePerResolution)]
[Register(typeof(ContainerViewModel), Scope.InstancePerResolution)]
[Register(typeof(DrawerViewModel), Scope.InstancePerResolution)]
[Register(typeof(AddCardsViewModel), Scope.InstancePerResolution)]
[Register(typeof(CardsDbContext), Scope.InstancePerResolution)]
[Register(typeof(CollectionTrackingService), Scope.InstancePerResolution, typeof(ICollectionTrackingService))]
[Register(typeof(ViewModelFactory), Scope.SingleInstance, typeof(IViewModelFactory))]
public partial class Container : IContainer<MainViewModel>
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
}
