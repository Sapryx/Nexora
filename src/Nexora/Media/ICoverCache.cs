using Avalonia.Media.Imaging;

namespace Nexora.Media;

public interface ICoverCache
{
    Bitmap? Get(string audioPath);
    Task<Bitmap?> GetOrLoadAsync(string audioPath);
    Task<Bitmap?> LoadLargeAsync(string audioPath);
}
