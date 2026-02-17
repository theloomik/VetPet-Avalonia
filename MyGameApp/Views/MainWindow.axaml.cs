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
        if (!(DataContext is MyGameApp.ViewModels.MainWindowViewModel vm) || !(sender is Button b))
            return;

        var idx = 0;
        if (b.Tag is string s && int.TryParse(s, out var parsed))
            idx = parsed;
        else if (b.Tag is int i)
            idx = i;

        // Передаємо індекс у ViewModel -> Вона сама оновить списки, очистить пошук і т.д.
        vm.SelectedIndex = idx;

        // Візуальне перемикання активної кнопки (Active class)
        var menu = this.FindControl<StackPanel>("MenuPanel");
        if (menu != null)
        {
            foreach (var child in menu.Children.OfType<Button>())
            {
                child.Classes.Remove("Active");
            }
            if (!b.Classes.Contains("Active"))
                b.Classes.Add("Active");
        }
    }
}