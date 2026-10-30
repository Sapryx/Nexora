using System.Text;
using Core.Playback.Loudness;
using Core.Storage.Tags;

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
        string path = CreateWav();

        saver.SaveLoudness(path, new TrackLoudness() { Lufs = -9.25, PeakDb = -6.0206 });

        using var tagFile = TagLib.File.Create(path);
        var loudness = LoudnessTags.Read(tagFile);
        Assert.Equal(-9.25, loudness!.Lufs, 6);
        Assert.Equal(-6.0206, loudness.PeakDb!.Value, 3);
    }

    [Fact]
    public void SaveLoudness_WavFile_LeavesNoTempFile()
    {
        string path = CreateWav();

        saver.SaveLoudness(path, new TrackLoudness() { Lufs = -14 });

        Assert.False(File.Exists(TagLibMetadataSaver.GetTempPath(path)));
        Assert.Single(Directory.GetFiles(directory));
    }

    [Fact]
    public void Save_TitleChange_WritesTitle()
    {
        string path = CreateWav();

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
        string path = CreateWav();
        File.WriteAllText(TagLibMetadataSaver.GetTempPath(path), "leftover");

        saver.SaveLoudness(path, new TrackLoudness() { Lufs = -14 });

        Assert.False(File.Exists(TagLibMetadataSaver.GetTempPath(path)));
        using var tagFile = TagLib.File.Create(path);
        Assert.NotNull(LoudnessTags.Read(tagFile));
    }

    private string CreateWav()
    {
        const int sampleRate = 48000;
        const short channels = 2;
        const short bitsPerSample = 16;
        int dataSize = sampleRate * channels * bitsPerSample / 8 / 10;
        string path = Path.Combine(directory, "track.wav");

        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        writer.Write(new byte[dataSize]);

        return path;
    }
}
