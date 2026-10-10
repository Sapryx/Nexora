using System.Collections.Generic;
using Core.Logging;
using Core.Search;
using Microsoft.Extensions.Logging;

namespace Nexora.Input;

public class UnsupportedKeyboardLayoutProvider : IKeyboardLayoutProvider
{
    private readonly ILogger logger;

    public UnsupportedKeyboardLayoutProvider(ILogger<UnsupportedKeyboardLayoutProvider> logger)
    {
        this.logger = logger;
    }

    public IReadOnlyList<KeyboardLayout> GetLayouts()
    {
        logger.Info($"Reading keyboard layouts is not supported on this platform");
        return [];
    }
}
