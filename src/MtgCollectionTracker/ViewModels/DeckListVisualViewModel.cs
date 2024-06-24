using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Services.Stubs;
using ScryfallApi.Client.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MtgCollectionTracker.ViewModels;

public partial class DeckListVisualViewModel : DrawerContentViewModel
{
    readonly ICollectionTrackingService _service;

    [ObservableProperty]
    private string _name;

    public DeckListVisualViewModel()
    {
        base.ThrowIfNotDesignMode();
        _service = new StubCollectionTrackingService();
        this.IsActive = true;
        this.Name = "Test Deck";
    }

    public DeckListVisualViewModel(ICollectionTrackingService service)
    {
        _service = service;
        this.IsActive = true;
    }

    public ObservableCollection<CardVisualViewModel> MainDeck { get; } = new();

    public ObservableCollection<CardVisualViewModel> Sideboard { get; } = new();

    [ObservableProperty]
    private int _mainDeckSize;

    [ObservableProperty]
    private int _sideboardSize;

    [ObservableProperty]
    private bool _isGrouped = true;

    public DeckListVisualViewModel WithDeck(DeckModel deck)
    {
        int mdTotal = 0;
        int sbTotal = 0;
        this.Name = deck.Name;
        if (this.IsGrouped)
        {
            var md = new List<CardVisualViewModel>(); 
            foreach (var grp in deck.MainDeck.GroupBy(c => c.SkuId))
            {
                var card = grp.First();
                using var ms = new MemoryStream(card.FrontFaceImage);
                var cm = new CardVisualViewModel { IsGrouped = this.IsGrouped, Quantity = grp.Count(), CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, IsProxy = card.IsProxy, CardImage = new Bitmap(ms) };
                md.Add(cm);
                mdTotal += grp.Count();
            }
            // Non-lands before lands
            foreach (var c in md.OrderBy(m => m.IsLand).ThenBy(m => m.Type))
                this.MainDeck.Add(c);

            foreach (var grp in deck.Sideboard.GroupBy(c => c.SkuId))
            {
                var card = grp.First();
                using var ms = new MemoryStream(card.FrontFaceImage);
                this.Sideboard.Add(new CardVisualViewModel { IsGrouped = this.IsGrouped, Quantity = grp.Count(), CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, IsProxy = card.IsProxy, CardImage = new Bitmap(ms) });
                sbTotal += grp.Count();
            }
        }
        else
        {
            var md = new List<CardVisualViewModel>();
            foreach (var card in deck.MainDeck)
            {
                using var ms = new MemoryStream(card.FrontFaceImage);
                var cm = new CardVisualViewModel { IsGrouped = this.IsGrouped, Quantity = 1, CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, IsProxy = card.IsProxy, CardImage = new Bitmap(ms) };
                md.Add(cm);
            }
            // Non-lands before lands
            foreach (var c in md.OrderBy(m => m.IsLand).ThenBy(m => m.Type))
                this.MainDeck.Add(c);

            foreach (var card in deck.Sideboard)
            {
                using var ms = new MemoryStream(card.FrontFaceImage);
                this.Sideboard.Add(new CardVisualViewModel { IsGrouped = this.IsGrouped, Quantity = 1, CardName = card.CardName, Type = card.Type, IsLand = card.IsLand, IsProxy = card.IsProxy, CardImage = new Bitmap(ms) });
            }

            mdTotal += this.MainDeck.Count;
            sbTotal += this.Sideboard.Count;
        }

        this.MainDeckSize = mdTotal;
        this.SideboardSize = sbTotal;

        return this;
    }
}
