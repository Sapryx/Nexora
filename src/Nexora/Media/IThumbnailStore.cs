using Avalonia.Media.Imaging;

namespace Nexora.Media;

public interface IThumbnailStore
{
    bool IsFresh(string audioPath, int pixelSide);
    bool TryLoad(string audioPath, int pixelSide, out Bitmap? thumbnail);
    void Save(string audioPath, int pixelSide, Bitmap? thumbnail);
    void RemoveOrphanedThumbnails(IEnumerable<string> audioPaths, CancellationToken token);
}
