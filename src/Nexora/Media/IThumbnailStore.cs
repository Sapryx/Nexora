using Avalonia.Media.Imaging;

namespace Nexora.Media;

public interface IThumbnailStore
{
    bool TryLoad(string audioPath, int pixelSide, out Bitmap? thumbnail);
    void Save(string audioPath, int pixelSide, Bitmap? thumbnail);
}
