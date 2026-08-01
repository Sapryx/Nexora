using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Nexora.Theme;

public class NexoraTheme : Styles
{
    public NexoraTheme()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
