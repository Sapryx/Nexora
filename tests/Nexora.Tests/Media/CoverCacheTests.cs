using Avalonia;
using Core.Storage.Tags;
using Microsoft.Extensions.Logging;
using Moq;
using Nexora.Media;
using Nexora.Render;
using Tests.Shared.Logging;

namespace Nexora.Tests.Media;

public class CoverCacheTests
{
    private const string AudioPath = "/music/broken.mp3";
    private readonly TestLogger<CoverCache> logger = new TestLogger<CoverCache>();
    private readonly Mock<ITrackCoverLoader> coverLoaderMock = new Mock<ITrackCoverLoader>();
    private readonly CoverCache coverCache;

    public CoverCacheTests()
    {
        coverCache = new CoverCache(logger, coverLoaderMock.Object, new RenderScaleProvider());
    }

    [Fact]
    public async Task GetOrLoadAsync_TrackWithoutCover_ReturnsNull()
    {
        coverLoaderMock.Setup(it => it.LoadCover(AudioPath)).Returns((byte[]?)null);
        var cover = await coverCache.GetOrLoadAsync(AudioPath);
        Assert.Null(cover);
    }

    [Fact]
    public async Task GetOrLoadAsync_LoaderThrows_ReturnsNull()
    {
        coverLoaderMock.Setup(it => it.LoadCover(AudioPath)).Throws(new IOException("Corrupted"));
        var cover = await coverCache.GetOrLoadAsync(AudioPath);
        Assert.Null(cover);
    }

    [Fact]
    public async Task GetOrLoadAsync_LoaderThrows_LogsWarningWithPathAndException()
    {
        var exception = new IOException("Corrupted");
        coverLoaderMock.Setup(it => it.LoadCover(AudioPath)).Throws(exception);

        await coverCache.GetOrLoadAsync(AudioPath);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains(AudioPath, entry.Message);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public async Task GetOrLoadAsync_LoaderThrewBefore_DoesNotLoadAgain()
    {
        coverLoaderMock.Setup(it => it.LoadCover(AudioPath)).Throws(new IOException("Corrupted"));
        await coverCache.GetOrLoadAsync(AudioPath);
        await coverCache.GetOrLoadAsync(AudioPath);
        coverLoaderMock.Verify(it => it.LoadCover(AudioPath), Times.Once);
    }

    [Fact]
    public async Task LoadLargeAsync_LoaderThrows_ReturnsNullAndLogsWarning()
    {
        coverLoaderMock.Setup(it => it.LoadCover(AudioPath)).Throws(new IOException("Corrupted"));

        var cover = await coverCache.LoadLargeAsync(AudioPath);

        Assert.Null(cover);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task LoadLargeAsync_CalledTwice_LoadsEachTime()
    {
        coverLoaderMock.Setup(it => it.LoadCover(AudioPath)).Returns((byte[]?)null);

        await coverCache.LoadLargeAsync(AudioPath);
        await coverCache.LoadLargeAsync(AudioPath);

        coverLoaderMock.Verify(it => it.LoadCover(AudioPath), Times.Exactly(2));
    }

    [Fact]
    public void FitToMaxSide_LargerThanMax_ScalesLargestSideToMaxKeepingAspect()
    {
        var size = CoverCache.FitToMaxSide(new PixelSize(4000, 3000), 2048);

        Assert.Equal(new PixelSize(2048, 1536), size);
    }

    [Fact]
    public void FitToMaxSide_NotLargerThanMax_KeepsSize()
    {
        var size = CoverCache.FitToMaxSide(new PixelSize(2048, 1000), 2048);

        Assert.Equal(new PixelSize(2048, 1000), size);
    }
}
