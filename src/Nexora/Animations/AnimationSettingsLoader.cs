using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Nexora.Animations;

public class AnimationSettingsLoader
{
    private readonly string defaultsJson;
    private readonly string? overridesFilePath;
    private readonly ILogger logger;

    public AnimationSettingsLoader(
        string defaultsJson,
        string? overridesFilePath,
        ILogger<AnimationSettingsLoader> logger
    )
    {
        this.defaultsJson = defaultsJson;
        this.overridesFilePath = overridesFilePath;
        this.logger = logger;
    }

    public AnimationSettings LoadDefaults()
    {
        return Deserialize(ParseObject(defaultsJson));
    }

    public AnimationSettings Load()
    {
        if(overridesFilePath == null || !File.Exists(overridesFilePath))
        {
            return LoadDefaults();
        }

        try
        {
            var settings = ParseObject(defaultsJson);
            Merge(settings, ParseObject(File.ReadAllText(overridesFilePath)));
            var result = Deserialize(settings);
            logger.Info($"Applied animation settings from {overridesFilePath}");
            return result;
        }
        catch(Exception ex)
        {
            logger.Warn(ex, $"Failed to apply animation settings from {overridesFilePath}, using defaults");
            return LoadDefaults();
        }
    }

    private static JsonObject ParseObject(string json)
    {
        return JsonNode.Parse(json) as JsonObject ?? throw new JsonException("Animation settings must be an object");
    }

    private static void Merge(JsonObject target, JsonObject overrides)
    {
        foreach(var pair in overrides)
        {
            if(target[pair.Key] is JsonObject targetGroup && pair.Value is JsonObject overrideGroup)
            {
                Merge(targetGroup, overrideGroup);
            }
            else
            {
                target[pair.Key] = pair.Value?.DeepClone();
            }
        }
    }

    private static AnimationSettings Deserialize(JsonObject json)
    {
        return json.Deserialize(AnimationSettingsJsonContext.Default.AnimationSettings)
            ?? throw new JsonException("Animation settings must be an object");
    }
}
