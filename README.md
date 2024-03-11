# mtg-collection-tracker

MtgCollectionTracker is a command-line based management tool for your Magic: The Gathering card collection

# How to build

 1. Clone this repo
 2. `dotnet restore`
 3. `dotnet build`
 4. `cd src/MtgCollectionTracker.Console`
 5. `dotnet run`

# Available commands

Run `MtgCollectionTracker.Console` with `--help` to see the available commands

```
  add-container            Adds a container to your collection

  add-deck                 Add a new deck to your collection

  add-to-container         Add cards to a container

  find-cards               Finds cards in your collection with the given name

  list-containers          Lists all containers in your collection

  list-decks               Lists all decks in your collection

  import                   Import a CSV of cards into your collection

  print-deck               Prints a given deck

  delete-card-sku          Delete a card sku from your collection. This is for if you entered a sku by mistake or you sold or traded away this
                           quantity of cards

  can-i-build-this-deck    Given a decklist in MTGO format, checks if you are able to build this deck with your current collection

  help                     Display more information on a specific command.

  version                  Display version information.
```