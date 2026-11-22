using System.Text;

namespace Tests.Shared.Storage;

public static class TestWavFile
{
    public static string Create(string directory)
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
