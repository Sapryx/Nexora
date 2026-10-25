namespace Nexora.ViewModels;

public class ArtistSuggestion
{
    public required string Name { get; init; }
    public string Match { get; init; } = "";
    public string Rest { get; init; } = "";
    public string Details { get; init; } = "";
    public bool IsNew { get; init; }
}
