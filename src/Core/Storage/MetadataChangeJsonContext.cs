using System.Text.Json.Serialization;

namespace Core.Storage;

[JsonSerializable(typeof(MetadataChange))]
internal partial class MetadataChangeJsonContext : JsonSerializerContext
{
}
