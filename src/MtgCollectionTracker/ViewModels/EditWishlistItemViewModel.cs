using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Core;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Contracts;
using MtgCollectionTracker.Services.Messaging;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtgCollectionTracker.ViewModels;

public partial class VendorOfferViewModel : ViewModelBase
{
    public IEnumerable<VendorViewModel> AvailableVendors { get; set; }

    [ObservableProperty]
    private VendorViewModel _vendor;

    [ObservableProperty]
    private int _availableStock;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string? _notes;
}

public partial class EditWishlistItemViewModel : DialogContentViewModel
{
    readonly ICollectionTrackingService _service;
    readonly IScryfallApiClient? _scryfallApiClient;
    public LanguageViewModel[] Languages { get; }

    public EditWishlistItemViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.AllTags = ["Foo", "Bar", "Baz"];
        this.Languages = [
            new LanguageViewModel("en", "en", "English"),
            new LanguageViewModel("es", "sp", "Spanish"),
            new LanguageViewModel("fr", "fr", "French"),
            new LanguageViewModel("de", "de", "German"),
            new LanguageViewModel("ja", "jp", "Japanese")
        ];
    }

    public EditWishlistItemViewModel(ICollectionTrackingService service, IViewModelFactory vmFactory, IScryfallApiClient scryfallApiClient)
    {
        _service = service;
        _scryfallApiClient = scryfallApiClient;
        this.AllTags = service.GetTags().ToList();
        this.Languages = service.GetLanguages().Select(lang => new LanguageViewModel(lang.Code, lang.PrintedCode, lang.Name)).ToArray();
    }

    private WishlistItemViewModel _origItem;

    public EditWishlistItemViewModel WithData(WishlistItemViewModel wm)
    {
        _origItem = wm;

        this.Id = wm.Id;
        this.CardName = wm.CardName;
        this.CollectorNumber = wm.CollectorNumber;
        this.Edition = wm.Edition;
        this.Language = this.Languages.FirstOrDefault(lang => lang.Code == wm.Language);
        this.Quantity = wm.RealQty;
        this.AvailableVendors = _service.GetVendors().Select(v => new VendorViewModel { Id = v.Id, Name = v.Name }).ToList();
        this.VendorOffers.Clear();
        foreach (var o in wm.Offers)
        {
            //HACK-ish: Fix up vendor reference so we're binding on the same identity
            if (o.Vendor != null)
            {
                var id = o.Vendor.Id;
                o.Vendor = this.AvailableVendors.First(v => v.Id == id);
            }

            o.AvailableVendors = this.AvailableVendors;
            this.VendorOffers.Add(o);
        }

        return this;
    }

    public IEnumerable<VendorViewModel> AvailableVendors { get; set; }

    public ObservableCollection<VendorOfferViewModel> VendorOffers { get; } = new();

    public int Id { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyCardName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyQuantity;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyEdition;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyLanguage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyCollector;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyOffers;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _applyTags;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _cardName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _edition;

    [ObservableProperty]
    private LanguageViewModel? _language;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int? _quantity;

    [ObservableProperty]
    private string? _collectorNumber;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool? _isFoil;

    private bool CanSave()
    {
        return this.ApplyCardName
            || this.ApplyCollector
            || this.ApplyEdition
            || this.ApplyLanguage
            || this.ApplyQuantity
            || this.ApplyTags
            || this.ApplyOffers
            || this.IsFoil.HasValue;
    }

    public List<string> AllTags { get; }

    public ObservableCollection<string> Tags { get; } = new();

    [RelayCommand]
    private void AddOffer()
    {
        VendorOffers.Add(new VendorOfferViewModel { AvailableVendors = AvailableVendors });
    }

    [RelayCommand]
    private void RemoveOffer(VendorOfferViewModel item)
    {
        VendorOffers.Remove(item);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save(CancellationToken cancel)
    {
        var m = new Core.Model.UpdateWishlistItemInputModel
        {
            Id = this.Id
        };
        if (!string.IsNullOrEmpty(CardName) && ApplyCardName)
            m.CardName = CardName;
        if (!string.IsNullOrEmpty(Edition) && ApplyEdition)
            m.Edition = Edition;
        if (Quantity > 0 && ApplyQuantity)
            m.Quantity = Quantity;
        if (Language != null && ApplyLanguage)
            m.Language = Language.Code;
        if (!string.IsNullOrEmpty(CollectorNumber) && ApplyCollector)
            m.CollectorNumber = CollectorNumber;
        m.ApplyTags = ApplyTags;
        if (ApplyTags)
            m.Tags = Tags.Distinct().ToList();

        m.IsFoil = this.IsFoil;

        if (ApplyOffers)
        {
            m.VendorOffers = this.VendorOffers.Select(v => new UpdateVendorOfferInputModel
            {
                VendorId = v.Vendor.Id,
                Available = v.AvailableStock,
                Price = v.Price,
                Notes = v.Notes
            });
        }

        var updated = await _service.UpdateWishlistItemAsync(m, _scryfallApiClient, cancel);

        Messenger.Send(new WishlistItemUpdatedMessage());

        if (_origItem != null)
        {
            _origItem.WithData(updated);
        }

        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private async Task CheckName()
    {
        if (!string.IsNullOrWhiteSpace(this.CardName) && _scryfallApiClient != null)
        {
            var (res, _) = await _scryfallApiClient.CheckCardNameAsync(this.CardName);
            if (res != null && this.CardName != res)
            {
                this.CardName = res;
                this.ApplyCardName = true;
                Messenger.ToastNotify("Card name fixed up");
            }
        }
    }
}
