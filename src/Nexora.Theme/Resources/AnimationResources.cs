using Avalonia.Controls;

namespace Nexora.Theme.Resources;

public class AnimationResources : ResourceDictionary
{
    public static IReadOnlyDictionary<string, object> Entries { get; set; } = new Dictionary<string, object>();

    public AnimationResources()
    {
        foreach(var pair in Entries)
        {
            Add(pair.Key, pair.Value);
        }
    }
}
