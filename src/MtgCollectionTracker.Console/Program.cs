using CommandLine;
using MtgCollectionTracker.Console.Commands;

var result = await Parser.Default
    .ParseArguments<
        AddContainerCommand,
        AddDeckCommand,
        AddToContainerCommand,
        FindCardsCommand,
        ListContainersCommand,
        ListDecksCommand,
        ImportCommand,
        PrintDeckCommand,
        DeleteCardSkuCommand,
        CanIBuildThisDeckCommand
    >(args)
    .MapResult(
    (AddContainerCommand options) => options.ExecuteAsync(),
    (AddDeckCommand options) => options.ExecuteAsync(),
    (AddToContainerCommand options) => options.ExecuteAsync(),
    (FindCardsCommand options) => options.ExecuteAsync(),
    (ListContainersCommand options) => options.ExecuteAsync(),
    (ListDecksCommand options) => options.ExecuteAsync(),
    (ImportCommand options) => options.ExecuteAsync(),
    (PrintDeckCommand options) => options.ExecuteAsync(),
    (DeleteCardSkuCommand options) => options.ExecuteAsync(),
    (CanIBuildThisDeckCommand options) => options.ExecuteAsync(),
    errors => ValueTask.FromResult(1));

return result;