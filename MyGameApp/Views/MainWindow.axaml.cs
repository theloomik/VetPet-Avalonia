using Avalonia.Controls;
using System.Linq;
namespace MyGameApp.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnMenuButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MyGameApp.ViewModels.MainWindowViewModel vm) return;
        if (sender is not Button b) return;

        var idx = 0;
        if (b.Tag is string s && int.TryParse(s, out var parsed))
            idx = parsed;

        vm.ChangeTab(idx);

        var menu = this.FindControl<StackPanel>("MenuPanel");
        if (menu != null)
        {
            foreach (var child in menu.Children.OfType<Button>())
                child.Classes.Remove("Active");
            b.Classes.Add("Active");
        }
    }
}