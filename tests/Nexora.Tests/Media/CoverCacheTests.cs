using Core.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Nexora.Media;
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
        coverCache = new CoverCache(logger, coverLoaderMock.Object);
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
}
