namespace MtgCollectionTracker.Core.Model;

/// <summary>
/// Represents a card during playtesting with its current state
/// </summary>
public class PlaytestCard
{
    public required string CardName { get; set; }
    
    public string? ScryfallId { get; set; }
    
    public string? ScryfallIdBack { get; set; }
    
    public string? ManaCost { get; set; }
    
    public string? CardType { get; set; }
    
    public string? Power { get; set; }
    
    public string? Toughness { get; set; }
    
    public string? OracleText { get; set; }
    
    public bool IsLand { get; set; }
    
    public bool IsDoubleFaced { get; set; }

    public bool IsToken { get; set; }
    
    public bool IsTapped { get; set; }
    
    public bool IsFrontFace { get; set; } = true;
    
    public GameZone Zone { get; set; }
    
    public string? PT
    {
        get
        {
            if (!string.IsNullOrEmpty(Power) && !string.IsNullOrEmpty(Toughness))
                return $"{Power}/{Toughness}";
            return null;
        }
    }
}
