using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nexora.Views;

namespace Nexora.UiTests.Views;

public class TextBoxFocusTests
{
    [AvaloniaFact]
    public void FocusedSearchBox_PressInsideAndOutside_KeepsThenClearsFocus()
    {
        using var app = new TestMainWindow();
        var textBox = app.Window.GetVisualDescendants().OfType<SearchBar>().Single()
            .GetVisualDescendants().OfType<TextBox>().First();
        textBox.Focus();
        Dispatcher.UIThread.RunJobs();

        app.Click(textBox);

        Assert.True(textBox.IsFocused);

        app.Click(app.Window.FindControl<TrackArea>("LibraryList")!);

        Assert.False(textBox.IsFocused);
    }
}
