using System.Text.Json.Serialization;

namespace Nexora.Animations;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    RespectNullableAnnotations = true
)]
[JsonSerializable(typeof(AnimationSettings))]
public partial class AnimationSettingsJsonContext : JsonSerializerContext
{
}
