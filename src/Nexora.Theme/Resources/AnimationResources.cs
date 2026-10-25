using Avalonia.Controls;

namespace Nexora.Theme.Resources;

public class AnimationResources : ResourceDictionary
{
    public static IReadOnlyDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

    public AnimationResources()
    {
        foreach(var pair in Values)
        {
            Add(pair.Key, pair.Value);
        }
    }
}
