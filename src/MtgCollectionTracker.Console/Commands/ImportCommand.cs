using CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MtgCollectionTracker.Core.Model;
using MtgCollectionTracker.Core.Services;
using MtgCollectionTracker.Data;
using System.Globalization;

namespace MtgCollectionTracker.Console.Commands;

record CsvImportRecord(int Qty, string CardName, string Edition, string? Language, bool? IsFoil, bool? IsLand, bool? IsSideboard, string? Condition, string? Comments);

[Verb("import", HelpText = "Import a CSV of cards into a given container or deck")]
internal class ImportCommand : CommandBase
{
    [Option("csv-path", Required = true, HelpText = "Path to CSV file. Must have schema of (Qty, CardName, Edition, Language?, IsSideboard?, IsFoil?, IsLand?, Condition?, Comments?)")]
    public required string CsvPath { get; set; }

    [Option("container-id", Required = false)]
    public int? ContainerId { get; set; }

    [Option("deck-id", Required = false)]
    public int? DeckId { get; set; }

    protected override async ValueTask<int> ExecuteInternalAsync(IServiceProvider serviceProvider)
    {
        var csvConf = new CsvConfiguration(CultureInfo.InvariantCulture);
        using var sr = new StreamReader(this.CsvPath);
        using var csvr = new CsvReader(sr, csvConf);

        var input = csvr.GetRecords<CsvImportRecord>()
            .Select(c => new AddToContainerInputModel
            {
                Quantity = c.Qty,
                CardName = c.CardName,
                Edition = c.Edition,
                Language = c.Language,
                IsSideboard = c.IsSideboard ?? false,
                IsFoil = c.IsFoil ?? false,
                IsLand = c.IsLand ?? false,
                Condition = TryParseCondition(c.Condition),
                Comments = c.Comments
            });

        var service = serviceProvider.GetRequiredService<CollectionTrackingService>();
        var (added, rows) = await service.AddMultipleToContainerOrDeckAsync(this.ContainerId, this.DeckId, input);

        Stdout($"{added} cards across {rows} rows added to container/deck");

        return 0;
    }

    static CardCondition? TryParseCondition(string? condition)
    {
        switch (condition?.ToLower())
        {
            case "nm":
                return CardCondition.NearMint;
            case "lp":
                return CardCondition.LightlyPlayed;
            case "mp":
                return CardCondition.ModeratelyPlayed;
            case "hp":
                return CardCondition.HeavilyPlayed;
        }
        return null;
    }
}
