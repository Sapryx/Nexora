using System.Collections.Immutable;

namespace Core.Storage;

public interface ISupportedAudioFormatsProvider
{
    public ImmutableHashSet<string> GetFormats();
}
