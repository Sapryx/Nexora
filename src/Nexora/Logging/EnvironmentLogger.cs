using System.Runtime.InteropServices;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Logging;

public class EnvironmentLogger
{
    private readonly ILogger<EnvironmentLogger> logger;
    private readonly bool isWaylandSession;

    public EnvironmentLogger(ILogger<EnvironmentLogger> logger, bool isWaylandSession)
    {
        this.logger = logger;
        this.isWaylandSession = isWaylandSession;
    }

    public void Log()
    {
        logger.Info(
            $"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}), " +
            $".NET {Environment.Version}"
        );
        logger.Info($"Windowing backend: {GetWindowingBackendName()}");

        if(OperatingSystem.IsLinux())
        {
            logger.Info(
                $"Session type: {GetVariable("XDG_SESSION_TYPE")}, " +
                $"desktop: {GetVariable("XDG_CURRENT_DESKTOP")}, " +
                $"WAYLAND_DISPLAY: {GetVariable("WAYLAND_DISPLAY")}, " +
                $"DISPLAY: {GetVariable("DISPLAY")}"
            );
        }
    }

    private string GetWindowingBackendName()
    {
        if(OperatingSystem.IsWindows())
        {
            return "Win32";
        }

        if(OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        return isWaylandSession ? "Wayland" : "X11";
    }

    private static string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? "<unset>";
    }
}
