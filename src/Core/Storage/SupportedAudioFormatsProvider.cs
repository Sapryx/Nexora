using System.Collections.Immutable;

namespace Core.Storage;

public class SupportedAudioFormatsProvider : ISupportedAudioFormatsProvider
{
    public ImmutableHashSet<string> GetFormats()
    {
        return
        [
            ".mp3",
            ".flac",
            ".wav",
            ".opus",
            ".ogg"
        ];
    }
}
