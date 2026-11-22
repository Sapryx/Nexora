using System.Text.Json.Nodes;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace Tests.Shared.Integrations;

public class FakeDiscordPipe : INamedPipeClient
{
    public ILogger Logger { get; set; } = new NullLogger();
    public int ConnectedPipe => 0;

    public bool IsConnected
    {
        get
        {
            lock(sync)
            {
                return isConnected;
            }
        }
    }

    public DiscordActivity? Activity
    {
        get
        {
            lock(sync)
            {
                return activity;
            }
        }
    }

    public bool ReceivedActivity
    {
        get
        {
            lock(sync)
            {
                return receivedActivity;
            }
        }
    }

    public int RefusedConnections
    {
        get
        {
            lock(sync)
            {
                return refusedConnections;
            }
        }
    }

    private const string ReadyMessage = """
        {
            "cmd": "DISPATCH",
            "evt": "READY",
            "data": {
                "v": 1,
                "config": {
                    "cdn_host": "cdn.discordapp.com",
                    "api_endpoint": "//discord.com/api",
                    "environment": "production"
                },
                "user": { "id": "1", "username": "user", "discriminator": "0" }
            }
        }
        """;

    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(15);
    private readonly object sync = new object();
    private readonly Queue<PipeFrame> incoming = new Queue<PipeFrame>();
    private bool isRunning;
    private bool isConnected;
    private bool receivedActivity;
    private int refusedConnections;
    private DiscordActivity? activity;

    public void Start()
    {
        lock(sync)
        {
            isRunning = true;
        }
    }

    public void Quit()
    {
        lock(sync)
        {
            isRunning = false;
            Disconnect();
        }
    }

    public void Reload()
    {
        lock(sync)
        {
            Disconnect();
        }
    }

    public void SendError(int code, string message)
    {
        var error = new JsonObject()
        {
            ["cmd"] = "SET_ACTIVITY",
            ["evt"] = "ERROR",
            ["data"] = new JsonObject() { ["code"] = code, ["message"] = message }
        };

        lock(sync)
        {
            incoming.Enqueue(CreateFrame(error.ToJsonString()));
        }
    }

    public void WaitUntil(Func<bool> condition)
    {
        if(!SpinWait.SpinUntil(condition, WaitTimeout))
        {
            throw new TimeoutException("The fake Discord did not reach the expected state");
        }
    }

    public bool Connect(int pipe)
    {
        lock(sync)
        {
            isConnected = isRunning;

            if(!isConnected)
            {
                refusedConnections++;
            }

            return isConnected;
        }
    }

    public bool ReadFrame(out PipeFrame frame)
    {
        lock(sync)
        {
            return incoming.TryDequeue(out frame);
        }
    }

    public bool WriteFrame(PipeFrame frame)
    {
        lock(sync)
        {
            if(!isConnected)
            {
                return false;
            }

            Receive(frame);
            return true;
        }
    }

    public void Close()
    {
        lock(sync)
        {
            Disconnect();
        }
    }

    public void Dispose()
    {
        Close();
    }

    private static PipeFrame CreateFrame(string message)
    {
        return new PipeFrame() { Opcode = Opcode.Frame, Message = message };
    }

    private static DiscordActivity? ParseActivity(JsonNode? node)
    {
        if(node == null)
        {
            return null;
        }

        return new DiscordActivity()
        {
            Details = node["details"]?.GetValue<string>(),
            State = node["state"]?.GetValue<string>()
        };
    }

    private void Receive(PipeFrame frame)
    {
        if(frame.Opcode == Opcode.Handshake)
        {
            incoming.Enqueue(CreateFrame(ReadyMessage));
            return;
        }

        var payload = JsonNode.Parse(frame.Message);

        if(frame.Opcode == Opcode.Frame && payload?["cmd"]?.GetValue<string>() == "SET_ACTIVITY")
        {
            SetActivity(payload);
        }
    }

    private void SetActivity(JsonNode payload)
    {
        var activityNode = payload["args"]?["activity"];
        activity = ParseActivity(activityNode);
        receivedActivity = true;

        var response = new JsonObject()
        {
            ["cmd"] = "SET_ACTIVITY",
            ["nonce"] = payload["nonce"]?.DeepClone(),
            ["data"] = activityNode?.DeepClone()
        };
        incoming.Enqueue(CreateFrame(response.ToJsonString()));
    }

    private void Disconnect()
    {
        isConnected = false;
        receivedActivity = false;
        activity = null;
        incoming.Clear();
    }
}
