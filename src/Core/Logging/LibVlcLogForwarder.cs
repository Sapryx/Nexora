using Microsoft.Extensions.Logging;
using VlcLogLevel = LibVLCSharp.Shared.LogLevel;

namespace Core.Logging;

public class LibVlcLogForwarder
{
    private readonly ILogger<LibVlcLogForwarder> logger;

    public LibVlcLogForwarder(ILogger<LibVlcLogForwarder> logger)
    {
        this.logger = logger;
    }

    public void Forward(VlcLogLevel level, string? module, string message)
    {
        switch(level)
        {
            case VlcLogLevel.Error:
                logger.Error($"(LibVLC) {module}: {message}");
                break;
            case VlcLogLevel.Warning:
            case VlcLogLevel.Notice:
                logger.Debug($"(LibVLC) {module}: {message}");
                break;
        }
    }
}
