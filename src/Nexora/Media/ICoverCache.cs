using Avalonia.Media.Imaging;

namespace Nexora.Media;

public interface ICoverCache
{
    Bitmap? Get(string audioPath, double size);
    Task<Bitmap?> GetOrLoadAsync(string audioPath, double size);
    Task<Bitmap?> LoadLargeAsync(string audioPath);
    Task<Bitmap?> LoadOriginalAsync(string audioPath);
}
