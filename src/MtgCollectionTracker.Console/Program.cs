using CommandLine;
using MtgCollectionTracker.Console.Commands;

var result = await Parser.Default
    .ParseArguments<AddToContainerCommand>(args)
    .MapResult(
    (AddToContainerCommand options) => options.ExecuteAsync(),
    errors => ValueTask.FromResult(1));

return result;