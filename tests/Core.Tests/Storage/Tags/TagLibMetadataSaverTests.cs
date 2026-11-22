using System.Text;
using Core.Storage.Tags;
using Core.Tracks;
using Tests.Shared.Storage;

namespace Core.Tests.Storage.Tags;

public class TagLibMetadataSaverTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-saver-{Guid.NewGuid()}");
    private readonly TagLibMetadataSaver saver = new TagLibMetadataSaver();

    public TagLibMetadataSaverTests()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void SaveLoudness_WavFile_WritesReplayGainTags()
    {
        string path = TestWavFile.Create(directory);

        saver.SaveLoudness(path, new TrackLoudness() { Lufs = -9.25, PeakDb = -6.0206 });

        using var tagFile = TagLib.File.Create(path);
        var loudness = LoudnessTags.Read(tagFile);
        Assert.Equal(-9.25, loudness!.Lufs, 6);
        Assert.Equal(-6.0206, loudness.PeakDb!.Value, 3);
    }

    [Fact]
    public void SaveLoudness_WavFile_LeavesNoTempFile()
    {
        string path = TestWavFile.Create(directory);

        saver.SaveLoudness(path, new TrackLoudness() { Lufs = -14 });

        Assert.False(File.Exists(TagLibMetadataSaver.GetTempPath(path)));
        Assert.Single(Directory.GetFiles(directory));
    }

    [Fact]
    public void Save_TitleChange_WritesTitle()
    {
        string path = TestWavFile.Create(directory);

        saver.Save(new MetadataChange() { AudioPath = path, Title = "Title" });

        using var tagFile = TagLib.File.Create(path);
        Assert.Equal("Title", tagFile.Tag.Title);
    }

    [Fact]
    public void SaveLoudness_CorruptFile_KeepsOriginalAndLeavesNoTempFile()
    {
        string path = Path.Combine(directory, "broken.mp3");
        var content = Encoding.ASCII.GetBytes("not an audio file");
        File.WriteAllBytes(path, content);

        Assert.ThrowsAny<Exception>(() => saver.SaveLoudness(path, new TrackLoudness() { Lufs = -14 }));

        Assert.Equal(content, File.ReadAllBytes(path));
        Assert.False(File.Exists(TagLibMetadataSaver.GetTempPath(path)));
    }

    [Fact]
    public void SaveLoudness_LeftoverTempFile_OverwritesItAndRemovesIt()
    {
        string path = TestWavFile.Create(directory);
        File.WriteAllText(TagLibMetadataSaver.GetTempPath(path), "leftover");

        saver.SaveLoudness(path, new TrackLoudness() { Lufs = -14 });

        Assert.False(File.Exists(TagLibMetadataSaver.GetTempPath(path)));
        using var tagFile = TagLib.File.Create(path);
        Assert.NotNull(LoudnessTags.Read(tagFile));
    }

    [Fact]
    public void SaveAndSaveLoudness_ConcurrentOnSameFile_BothLand()
    {
        string path = TestWavFile.Create(directory);

        Parallel.For(0, 100, index =>
        {
            if(index % 2 == 0)
            {
                saver.Save(new MetadataChange() { AudioPath = path, Title = "Title" });
            }
            else
            {
                saver.SaveLoudness(path, new TrackLoudness() { Lufs = -14 });
            }
        });

        using var tagFile = TagLib.File.Create(path);
        Assert.Equal("Title", tagFile.Tag.Title);
        Assert.Equal(-14, LoudnessTags.Read(tagFile)!.Lufs, 6);
    }
}
