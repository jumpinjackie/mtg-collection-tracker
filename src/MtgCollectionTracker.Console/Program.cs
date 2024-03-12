using CommandLine;
using MtgCollectionTracker.Console;
using MtgCollectionTracker.Console.Commands;

var result = await Parser.Default
    .ParseArguments<
        AddContainerCommand,
        AddDeckCommand,
        AddCardSkuCommand,
        FindCardsCommand,
        ListContainersCommand,
        ListDecksCommand,
        ImportCommand,
        PrintDeckCommand,
        DeleteCardSkuCommand,
        CanIBuildThisDeckCommand,
        DismantleDeckCommand,
        SplitCardSkuCommand,
        UpdateCardSkuCommand,
        RemoveFromDeckCommand,
        ConsolidateSkusCommand
    >(args)
    .MapResult(
    (CommandBase cmd) => cmd.ExecuteAsync(),
    errors => ValueTask.FromResult(1));

return result;