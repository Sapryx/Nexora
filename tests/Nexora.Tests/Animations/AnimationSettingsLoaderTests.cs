using Microsoft.Extensions.Logging;
using Nexora.Animations;
using Tests.Shared.Logging;

namespace Nexora.Tests.Animations;

public class AnimationSettingsLoaderTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-animations-{Guid.NewGuid()}");
    private readonly string filePath;
    private readonly TestLogger<AnimationSettingsLoader> logger = new TestLogger<AnimationSettingsLoader>();
    private readonly AnimationSettingsLoader loader;

    public AnimationSettingsLoaderTests()
    {
        filePath = Path.Combine(directory, "animations.json");
        loader = new AnimationSettingsLoader(ReadDefaults(), filePath, logger);
    }

    public void Dispose()
    {
        if(Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void LoadDefaults_AssetFile_ReturnsAllGroups()
    {
        var settings = loader.LoadDefaults();

        Assert.Equal(150, settings.Popup.OpenDuration);
        Assert.Equal(120, settings.ContextMenu.OpenDuration);
        Assert.Equal(200, settings.SegmentedControl.SlideDuration);
        Assert.Equal("#23FFFFFF", settings.Track.HoverColor);
        Assert.Equal(0.5, settings.Track.Cover.HoverOpacity);
        Assert.Equal("CubicEaseOut", settings.Default.EaseOut);
    }

    [Fact]
    public void LoadDefaults_KeyMissing_Throws()
    {
        var incomplete = new AnimationSettingsLoader("{ \"popup\": {} }", null, logger);

        Assert.ThrowsAny<Exception>(() => incomplete.LoadDefaults());
    }

    [Fact]
    public void Load_FileMissing_ReturnsDefaultsWithoutCreatingFile()
    {
        var settings = loader.Load();

        Assert.Equal(150, settings.Popup.OpenDuration);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void Load_NoFilePath_ReturnsDefaults()
    {
        var withoutOverrides = new AnimationSettingsLoader(ReadDefaults(), null, logger);

        Assert.Equal(150, withoutOverrides.Load().Popup.OpenDuration);
    }

    [Fact]
    public void Load_ValueOverridden_ReturnsOverriddenValue()
    {
        WriteFile("{ \"popup\": { \"open_duration\": 1000 }, \"default\": { \"ease_out\": \"BackEaseOut\" } }");

        var settings = loader.Load();

        Assert.Equal(1000, settings.Popup.OpenDuration);
        Assert.Equal("BackEaseOut", settings.Default.EaseOut);
    }

    [Fact]
    public void Load_KeyMissingInFile_KeepsDefault()
    {
        WriteFile("{ \"popup\": { \"open_duration\": 1000 } }");

        var settings = loader.Load();

        Assert.Equal(100, settings.Popup.CloseDuration);
        Assert.Equal(200, settings.SegmentedControl.SlideDuration);
        Assert.Equal(100, settings.Track.Cover.HoverDuration);
    }

    [Fact]
    public void Load_UnknownKey_IgnoresKey()
    {
        WriteFile("{ \"popup\": { \"open_duration\": 300, \"bounce\": 5 }, \"unknown\": {} }");

        var settings = loader.Load();

        Assert.Equal(300, settings.Popup.OpenDuration);
    }

    [Theory]
    [InlineData("{ \"popup\": ")]
    [InlineData("{ \"popup\": { \"open_duration\": \"fast\" } }")]
    [InlineData("{ \"popup\": null }")]
    [InlineData("null")]
    public void Load_InvalidJson_LogsWarningAndReturnsDefaults(string json)
    {
        WriteFile(json);

        var settings = loader.Load();

        Assert.Equal(150, settings.Popup.OpenDuration);
        Assert.Contains(logger.Entries, it => it.Level == LogLevel.Warning && it.Message.Contains(filePath));
    }

    public static string ReadDefaults()
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "animations.json"));
    }

    private void WriteFile(string text)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, text);
    }
}
