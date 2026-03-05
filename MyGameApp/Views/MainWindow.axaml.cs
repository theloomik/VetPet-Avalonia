using Avalonia.Controls;
using MyGameApp.ViewModels;
using System.ComponentModel;
using System.Linq;
namespace MyGameApp.Views;
public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
        AttachViewModel();
    }

    private void OnMenuButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MyGameApp.ViewModels.MainWindowViewModel vm) return;
        if (sender is not Button b) return;

        var idx = 0;
        if (b.Tag is string s && int.TryParse(s, out var parsed))
            idx = parsed;

        vm.ChangeTab(idx);
        SetActiveMenuByIndex(idx);
    }

    private void AttachViewModel()
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;

        _vm = DataContext as MainWindowViewModel;
        if (_vm == null)
            return;

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        SetActiveMenuByViewModel(_vm.CurrentViewModel);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.CurrentViewModel) || _vm == null)
            return;

        SetActiveMenuByViewModel(_vm.CurrentViewModel);
    }

    private void SetActiveMenuByViewModel(ViewModelBase? currentViewModel)
    {
        var idx = currentViewModel switch
        {
            ClientsViewModel => 0,
            ClientDetailsViewModel => 0,
            AppointmentsViewModel => 1,
            QuickAppointmentViewModel => 1,
            StaffViewModel => 2,
            StaffDetailsViewModel => 2,
            ProvidersViewModel => 3,
            ProviderDetailsViewModel => 3,
            StockViewModel => 4,
            _ => 0
        };

        SetActiveMenuByIndex(idx);
    }

    private void SetActiveMenuByIndex(int idx)
    {
        var menu = this.FindControl<StackPanel>("MenuPanel");
        if (menu != null)
        {
            foreach (var child in menu.Children.OfType<Button>())
                child.Classes.Remove("Active");

            var active = menu.Children
                .OfType<Button>()
                .FirstOrDefault(btn => btn.Tag?.ToString() == idx.ToString());

            active?.Classes.Add("Active");
        }
    }
}
