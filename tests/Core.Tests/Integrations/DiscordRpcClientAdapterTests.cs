using Core.Integrations;
using Core.Logging;
using DiscordRPC;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared.Integrations;

namespace Core.Tests.Integrations;

[Trait("Category", "Slow")]
[Trait("Covers", "src/Core/Integrations/DiscordRpcClientAdapter.cs")]
[Trait("Covers", "tests/Tests.Shared/Integrations")]
public sealed class DiscordRpcClientAdapterTests : IDisposable
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(15);
    private readonly FakeDiscordPipe pipe = new FakeDiscordPipe();
    private readonly DiscordRpcClientAdapter adapter;

    public DiscordRpcClientAdapterTests()
    {
        adapter = new DiscordRpcClientAdapter(pipe, NullLogger<DiscordRpcLogger>.Instance);
    }

    [Fact]
    public async Task Initialize_DiscordRunning_RaisesReadyWithUsername()
    {
        TaskCompletionSource<string> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.Ready += username => ready.TrySetResult(username);
        pipe.Start();

        adapter.Initialize();

        Assert.Equal("user", await ready.Task.WaitAsync(EventTimeout));
    }

    [Fact]
    public async Task Initialize_DiscordClosed_RaisesConnectionFailed()
    {
        TaskCompletionSource failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.ConnectionFailed += () => failed.TrySetResult();

        adapter.Initialize();

        await failed.Task.WaitAsync(EventTimeout);
    }

    [Fact]
    public async Task Error_SentByDiscord_RaisesErrorWithCodeAndMessage()
    {
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<string> error = new(TaskCreationOptions.RunContinuationsAsynchronously);
        adapter.Ready += _ => ready.TrySetResult();
        adapter.Error += (code, message) => error.TrySetResult($"{code} {message}");
        pipe.Start();
        adapter.Initialize();
        await ready.Task.WaitAsync(EventTimeout);

        pipe.SendError(4000, "Bad activity");

        Assert.Equal("4000 Bad activity", await error.Task.WaitAsync(EventTimeout));
    }

    [Fact]
    public void SetPresence_DiscordRunning_ShowsPresence()
    {
        pipe.Start();
        adapter.Initialize();

        adapter.SetPresence(new RichPresence() { Details = "Psycho", State = "Muse" });

        pipe.WaitUntil(() => pipe.Activity != null);
        Assert.Equal("Psycho", pipe.Activity?.Details);
        Assert.Equal("Muse", pipe.Activity?.State);
    }

    [Fact]
    public void Reconnect_PresenceSet_ShowsPresenceAgain()
    {
        pipe.Start();
        adapter.Initialize();
        adapter.SetPresence(new RichPresence() { Details = "Psycho", State = "Muse" });
        pipe.WaitUntil(() => pipe.Activity != null);

        pipe.Reload();

        pipe.WaitUntil(() => pipe.ReceivedActivity);
        Assert.Equal("Psycho", pipe.Activity?.Details);
    }

    public void Dispose()
    {
        adapter.Dispose();
    }
}
