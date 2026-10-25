using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Transformation;

namespace Nexora.Controls;

public class SegmentedControl : ListBox
{
    private Border? indicator;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        indicator = e.NameScope.Find<Border>("PART_Indicator");
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        UpdateIndicator();
        return size;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if(change.Property == SelectedIndexProperty)
        {
            UpdateIndicator();
        }
    }

    private void UpdateIndicator()
    {
        if(indicator == null)
        {
            return;
        }

        var container = ContainerFromIndex(SelectedIndex);
        indicator.IsVisible = container != null;

        if(container == null)
        {
            return;
        }

        indicator.Width = container.Bounds.Width;
        indicator.Height = container.Bounds.Height;

        var transform = TransformOperations.CreateBuilder(1);
        transform.AppendTranslate(container.Bounds.X, container.Bounds.Y);
        indicator.RenderTransform = transform.Build();
    }
}
