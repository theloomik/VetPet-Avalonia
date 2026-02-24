using Avalonia.Controls;
using Avalonia.Interactivity;
using MyGameApp.Models;
using MyGameApp.ViewModels;

namespace MyGameApp.Views;

public partial class ClientsView : UserControl
{
    public ClientsView()
    {
        InitializeComponent();
    }

    private void OnDetailsClick(object? sender, RoutedEventArgs e)
    {
        // 1. Отримуємо DataContext самої кнопки (це має бути об'єкт Client зі списку)
        if (sender is Button btn && btn.DataContext is Client client)
        {
            // 2. Отримуємо DataContext всього UserControl (це має бути ClientsViewModel)
            if (DataContext is ClientsViewModel vm)
            {
                // 3. Викликаємо команду, яка перекидає нас на деталі, передаючи їй клієнта
                vm.GoToDetails(client);
            }
        }
    }
}