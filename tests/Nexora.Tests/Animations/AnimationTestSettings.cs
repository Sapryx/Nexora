namespace Nexora.Tests.Animations;

public static class AnimationTestSettings
{
    public const string Json =
        """
        {
            "popup": { "open_duration": 150, "close_duration": 100, "initial_scale": 0.95, "backdrop_opacity": 0.6 },
            "context_menu": { "open_duration": 120 },
            "segmented_control": { "slide_duration": 200 },
            "track": {
                "hover_duration": 100,
                "hover_color": "#23FFFFFF",
                "cover": { "hover_duration": 100, "hover_opacity": 0.5 }
            },
            "default": { "ease_in": "CubicEaseIn", "ease_out": "CubicEaseOut" }
        }
        """;

    public static string ReadAsset()
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "animations.json"));
    }
}
