using Avalonia;
using Avalonia.Controls;

namespace Nexora.Controls;

public partial class Icon : UserControl
{
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<Icon, string?>(nameof(IconName));

    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }
    
    public Icon()
    {
        InitializeComponent();
    }
}
