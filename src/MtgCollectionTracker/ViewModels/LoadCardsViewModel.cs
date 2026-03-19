using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MtgCollectionTracker.Services;
using MtgCollectionTracker.Services.Messaging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MtgCollectionTracker.ViewModels;

public readonly record struct LoadCardsInputItem(int Qty, string CardName, string? Edition, string? CollectorNumber, bool IsFoil);

public partial class LoadCardsViewModel : DialogContentViewModel
{
    static readonly Regex DetailedFormat = new(
        "^(?<qty>\\d+)\\s+(?<name>.+?)\\s+\\((?<edition>[^)]+)\\)\\s+(?<collector>\\S+)(?:\\s+(?<foil>\\*?F\\*?))?\\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    static readonly Regex BasicFormat = new(
        "^(?<qty>\\d+)\\s+(?<name>.+?)\\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Action<IReadOnlyCollection<LoadCardsInputItem>> _onLoadCompleted = static _ => { };

    public LoadCardsViewModel()
    {
    }

    public LoadCardsViewModel(IMessenger messenger)
        : base(messenger)
    {
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private string _decklistText = string.Empty;

    public LoadCardsViewModel WithOnLoadCompleted(Action<IReadOnlyCollection<LoadCardsInputItem>> onLoadCompleted)
    {
        _onLoadCompleted = onLoadCompleted;
        return this;
    }

    private bool CanLoad => !string.IsNullOrWhiteSpace(DecklistText);

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private void Load()
    {
        var parsedCards = new List<LoadCardsInputItem>();
        var parseErrors = new List<int>();

        var lines = DecklistText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
                continue;

            if (!TryParseLine(line, out var parsedCard))
            {
                parseErrors.Add(i + 1);
                continue;
            }

            parsedCards.Add(parsedCard);
        }

        if (parseErrors.Count > 0)
        {
            Messenger.ToastNotify($"Could not parse decklist line(s): {string.Join(", ", parseErrors)}", NotificationType.Error);
            return;
        }

        if (parsedCards.Count == 0)
        {
            Messenger.ToastNotify("No cards were found in the provided decklist", NotificationType.Warning);
            return;
        }

        _onLoadCompleted(parsedCards);
        Messenger.Send(new CloseDialogMessage());
    }

    [RelayCommand]
    private void Cancel()
    {
        Messenger.Send(new CloseDialogMessage());
    }

    private static bool TryParseLine(string line, out LoadCardsInputItem parsedCard)
    {
        var detailedMatch = DetailedFormat.Match(line);
        if (detailedMatch.Success
            && int.TryParse(detailedMatch.Groups["qty"].Value, out var qty)
            && qty > 0)
        {
            var name = detailedMatch.Groups["name"].Value.Trim();
            var edition = detailedMatch.Groups["edition"].Value.Trim();
            var collectorNumber = detailedMatch.Groups["collector"].Value.Trim();
            var foilValue = detailedMatch.Groups["foil"].Success ? detailedMatch.Groups["foil"].Value : string.Empty;
            var isFoil = foilValue.Trim('*').Equals("F", StringComparison.OrdinalIgnoreCase);

            if (name.Length > 0 && edition.Length > 0 && collectorNumber.Length > 0)
            {
                parsedCard = new LoadCardsInputItem(qty, name, edition, collectorNumber, isFoil);
                return true;
            }
        }

        var basicMatch = BasicFormat.Match(line);
        if (basicMatch.Success
            && int.TryParse(basicMatch.Groups["qty"].Value, out qty)
            && qty > 0)
        {
            var name = basicMatch.Groups["name"].Value.Trim();
            if (name.Length > 0)
            {
                parsedCard = new LoadCardsInputItem(qty, name, null, null, false);
                return true;
            }
        }

        parsedCard = default;
        return false;
    }
}
