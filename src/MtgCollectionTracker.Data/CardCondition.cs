namespace MtgCollectionTracker.Data;

/// <summary>
/// Card conditions as defined by
/// 
/// https://help.tcgplayer.com/hc/en-us/articles/221430307-How-can-I-tell-what-condition-a-card-is-in
/// </summary>
public enum CardCondition
{
    NearMint,
    LightlyPlayed,
    ModeratelyPlayed,
    HeavilyPlayed,
    Damaged
}
