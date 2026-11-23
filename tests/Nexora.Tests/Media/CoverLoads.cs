using Avalonia.Media.Imaging;
using Moq;
using Nexora.Media;

namespace Nexora.Tests.Media;

public sealed class CoverLoad
{
    public string AudioPath { get; }
    public double Size { get; }
    public CancellationToken Token { get; }
    public TaskCompletionSource<Bitmap?> Completion { get; } = new TaskCompletionSource<Bitmap?>();

    public CoverLoad(string audioPath, double size, CancellationToken token)
    {
        AudioPath = audioPath;
        Size = size;
        Token = token;
    }
}

public sealed class CoverLoads
{
    public Mock<ICoverCache> CoverCacheMock { get; } = new Mock<ICoverCache>();
    public List<CoverLoad> Started { get; } = [];
    public IEnumerable<string> StartedPaths => Started.Select(load => load.AudioPath);

    public CoverLoads()
    {
        CoverCacheMock
            .Setup(it => it.GetOrLoadAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns<string, double, CancellationToken>((audioPath, size, token) =>
            {
                var load = new CoverLoad(audioPath, size, token);
                Started.Add(load);
                return load.Completion.Task;
            });
    }

    public CoverLoad Of(string audioPath)
    {
        return Started.Single(load => load.AudioPath == audioPath);
    }
}
