using Core.Library;
using Core.Storage.Tags;
using Core.Tracks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexora.Media;
using Tests.Shared.Library;
using Tests.Shared.Playback;

namespace Nexora.Tests.Media;

public sealed class ThumbnailGeneratorTests : IDisposable
{
    private readonly List<IAudioTrack> tracks = [
        new AudioTrack("/music/Psycho.mp3", new Metadata() { Title = "Psycho" }),
        new AudioTrack("/music/Uprising.mp3", new Metadata() { Title = "Uprising" })
    ];

    private readonly Mock<ICoverCache> coverCacheMock = new Mock<ICoverCache>();
    private readonly Mock<IThumbnailStore> thumbnailStoreMock = new Mock<IThumbnailStore>();
    private readonly MusicLibrary library;
    private readonly ThumbnailGenerator generator;

    public ThumbnailGeneratorTests()
    {
        var tagWriter = new TrackTagWriter(
            NullLogger<TrackTagWriter>.Instance,
            Mock.Of<IMetadataSaver>(),
            AudioPlayerMock.Create().Object
        );
        library = TestMusicLibrary.Create(tagWriter, tracks);
        generator = new ThumbnailGenerator(
            NullLogger<ThumbnailGenerator>.Instance,
            library,
            coverCacheMock.Object,
            thumbnailStoreMock.Object
        );
    }

    [Fact]
    public async Task Start_LibraryLoaded_RemovesOrphansBeforeCreatingThumbnailsInLibraryOrder()
    {
        var createdPaths = new List<string>();
        List<string>? retainedPaths = null;
        thumbnailStoreMock
            .Setup(it => it.RemoveOrphanedThumbnails(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<string> paths, CancellationToken _) =>
            {
                Assert.Empty(createdPaths);
                retainedPaths = paths.ToList();
            });
        coverCacheMock
            .Setup(it => it.CreateThumbnails(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string audioPath, CancellationToken _) => createdPaths.Add(audioPath));

        generator.Start();
        generator.Start();
        await library.LoadAsync();
        await generator.Completion;

        Assert.Equal(["/music/Psycho.mp3", "/music/Uprising.mp3"], createdPaths);
        Assert.Equal(createdPaths, retainedPaths);
    }

    [Fact]
    public async Task LibraryLoaded_NotStarted_CreatesNothing()
    {
        await library.LoadAsync();
        await generator.Completion;

        coverCacheMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_AfterDisposal_DoesNotSubscribeToLibrary()
    {
        generator.Dispose();
        generator.Start();
        await library.LoadAsync();

        Assert.True(generator.Completion.IsCompletedSuccessfully);
        coverCacheMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Dispose_DuringLibraryLoadedEvent_DoesNotStartGeneration()
    {
        library.Loaded += _ => generator.Dispose();
        generator.Start();
        await library.LoadAsync();

        Assert.True(generator.Completion.IsCompletedSuccessfully);
        coverCacheMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LibraryLoaded_OverlappingNotifications_StartOnlyOneGeneration()
    {
        using var entered = new SemaphoreSlim(0);
        using var release = new SemaphoreSlim(0);
        library.Loaded += _ =>
        {
            entered.Release();
            release.Wait();
        };
        generator.Start();
        var firstLoad = library.LoadAsync();
        var secondLoad = library.LoadAsync();
        await entered.WaitAsync();
        await entered.WaitAsync();
        release.Release(2);
        await Task.WhenAll(firstLoad, secondLoad);
        await generator.Completion;

        coverCacheMock.Verify(
            it => it.CreateThumbnails(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(tracks.Count)
        );
    }

    [Fact]
    public async Task Dispose_WhileCreating_WaitsForCurrentTrackAndSkipsTheRest()
    {
        using var entered = new SemaphoreSlim(0);
        using var release = new SemaphoreSlim(0);
        var passedToken = CancellationToken.None;
        coverCacheMock
            .Setup(it => it.CreateThumbnails(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CancellationToken token) =>
            {
                passedToken = token;
                entered.Release();
                release.Wait();
            });
        generator.Start();
        await library.LoadAsync();
        await entered.WaitAsync();

        var disposing = Task.Run(generator.Dispose);
        await Task.Run(() => SpinWait.SpinUntil(() => passedToken.IsCancellationRequested));
        Assert.False(disposing.IsCompleted);
        release.Release();
        await disposing;

        Assert.True(generator.Completion.IsCompleted);
        coverCacheMock.Verify(
            it => it.CreateThumbnails(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Dispose_LibraryLoadedAfterwards_CreatesNothing()
    {
        generator.Start();
        generator.Dispose();

        await library.LoadAsync();
        await generator.Completion;

        coverCacheMock.VerifyNoOtherCalls();
    }

    public void Dispose()
    {
        generator.Dispose();
    }
}
