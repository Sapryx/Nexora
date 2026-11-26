using Avalonia.Media.Imaging;

namespace Nexora.Media;

public interface ICoverCache
{
    long MemoryBudget { get; set; }
    long MemoryUsed { get; }

    Bitmap? Get(string audioPath, double size);
    Task<Bitmap?> GetOrLoadAsync(string audioPath, double size, CancellationToken token);
    void CreateThumbnails(string audioPath, CancellationToken token);
    Task<Bitmap?> LoadLargeAsync(string audioPath);
    Task<Bitmap?> LoadOriginalAsync(string audioPath);
}
