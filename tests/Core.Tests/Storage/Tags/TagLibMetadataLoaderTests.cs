using Core.Storage.Tags;

namespace Core.Tests.Storage.Tags;

public class TagLibMetadataLoaderTests
{
    [Fact]
    public void SplitArtists_CommaSeparated_ReturnsEachArtist()
    {
        var artists = TagLibMetadataLoader.SplitArtists(["amies, Lofi Girl, Arthur Spuijman"]);

        Assert.Equal(["amies", "Lofi Girl", "Arthur Spuijman"], artists);
    }

    [Fact]
    public void SplitArtists_AmpersandSeparated_ReturnsEachArtist()
    {
        var artists = TagLibMetadataLoader.SplitArtists(["Geoxor & SVRGE"]);

        Assert.Equal(["Geoxor", "SVRGE"], artists);
    }

    [Fact]
    public void SplitArtists_SeveralPerformers_KeepsOrderAndSkipsEmptyAndDuplicates()
    {
        var artists = TagLibMetadataLoader.SplitArtists(["Queen, ", "David Bowie", "queen"]);

        Assert.Equal(["Queen", "David Bowie"], artists);
    }
}
