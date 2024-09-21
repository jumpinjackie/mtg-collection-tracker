using System.Text.RegularExpressions;

namespace MtgCollectionTracker.Core.Services;

public record DecklistEntry(int Quantity, string CardName, bool IsSideboard);

public class DecklistReader
{
    static Regex smEntryLine = new("(\\d+) (.+)");

    public IEnumerable<DecklistEntry> ReadDecklist(IEnumerable<string> lines)
    {
        bool isSideboard = false;
        foreach (var line in lines)
        {
            // A line containing the word "sideboard" should be sufficient indicator that the sideboard
            // section is about to begin...
            //
            // ...Until wotc stupidly prints a new card with that word in its name!
            if (line.Contains("sideboard", StringComparison.OrdinalIgnoreCase))
            {
                isSideboard = true;
                continue;
            }
            else
            {
                if (TryParseLine(line, isSideboard, out var ent))
                {
                    yield return ent;
                }
            }
        }
    }

    public IEnumerable<DecklistEntry> ReadDecklist(TextReader tr)
    {
        return ReadDecklist(GetLines(tr));

        IEnumerable<string> GetLines(TextReader tr)
        {
            string? line = tr.ReadLine();
            while (line != null)
            {
                yield return line;
                line = tr.ReadLine();
            }
        }
    }

    bool TryParseLine(string line, bool isSideboard, out DecklistEntry entry)
    {
        entry = new DecklistEntry(0, string.Empty, isSideboard);

        // A quantity entry should be of the form:
        //
        //   * n CARDNAME
        //   * nx CARDNAME
        //
        // Which this regex should catch
        var m = smEntryLine.Match(line);
        if (m.Groups.Count == 3)
        {
            // First group should be the quantity
            if (int.TryParse(m.Groups[1].ValueSpan, out var qty))
            {
                entry = entry with { Quantity = qty, CardName = m.Groups[2].Value.Trim() };
                return true;
            }
        }

        return false;
    }
}
