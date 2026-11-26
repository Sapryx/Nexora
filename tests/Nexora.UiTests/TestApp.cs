using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nexora.UiTests;

public class TestApp : App
{
    [SetsRequiredMembers]
    public TestApp()
    {
        LoadResources = () => CompositionRoot.LoadAnimationResources(NullLoggerFactory.Instance);
        Start = () => throw new InvalidOperationException("UI tests open the main window through TestMainWindow");
    }
}
