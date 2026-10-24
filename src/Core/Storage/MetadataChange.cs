namespace Core.Storage;

public class MetadataChange
{
    public required string AudioPath { get; init; }

    /// <summary>The new title, or <c>null</c> to keep the current one.</summary>
    public string? Title { get; init; }

    /// <summary>The new comma-separated artists, or <c>null</c> to keep the current ones.</summary>
    public string? Artists { get; init; }
}
