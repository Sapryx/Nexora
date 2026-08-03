using Avalonia.Controls;
using Nexora.ViewModels;

namespace Nexora.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if(DataContext is MainWindowVm vm)
        {
            vm.UpdateLayout(e.NewSize.Width);
        }
    }
}
