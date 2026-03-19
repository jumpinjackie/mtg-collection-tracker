namespace MtgCollectionTracker.Core.Model;

/// <summary>
/// Represents the result of validating a Commander deck against Commander rules
/// </summary>
public class CommanderValidationResult
{
    /// <summary>
    /// Whether the Commander deck is valid
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// The list of validation errors found
    /// </summary>
    public List<string> Errors { get; } = new();
}
