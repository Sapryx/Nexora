using Core.Integrations;
using Core.Logging;
using DiscordRPC;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared.Integrations;

namespace Core.Tests.Integrations;

public sealed class DiscordRpcClientAdapterTests : IDisposable
{
    private readonly FakeDiscordPipe pipe = new FakeDiscordPipe();
    private readonly DiscordRpcClientAdapter adapter;

    public DiscordRpcClientAdapterTests()
    {
        adapter = new DiscordRpcClientAdapter(pipe, NullLogger<DiscordRpcLogger>.Instance);
    }

    [Fact]
    public void Initialize_DiscordRunning_RaisesReadyWithUsername()
    {
        string? username = null;
        adapter.Ready += it => username = it;
        pipe.Start();

        adapter.Initialize();

        pipe.WaitUntil(() => username != null);
        Assert.Equal("user", username);
    }

    [Fact]
    public void Initialize_DiscordClosed_RaisesConnectionFailed()
    {
        bool failed = false;
        adapter.ConnectionFailed += () => failed = true;

        adapter.Initialize();

        pipe.WaitUntil(() => failed);
    }

    [Fact]
    public void Error_SentByDiscord_RaisesErrorWithCodeAndMessage()
    {
        string? error = null;
        adapter.Error += (code, message) => error = $"{code} {message}";
        pipe.Start();
        adapter.Initialize();
        pipe.WaitUntil(() => pipe.IsConnected);

        pipe.SendError(4000, "Bad activity");

        pipe.WaitUntil(() => error != null);
        Assert.Equal("4000 Bad activity", error);
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
