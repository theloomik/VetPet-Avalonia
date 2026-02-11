namespace MyGameApp.ViewModels;

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MyGameApp.Models;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Hello";

    public string ConnectionStatus { get; private set; } = "Not initialized";

    private readonly MySqlService _db;

    private int _selectedIndex;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (SetProperty(ref _selectedIndex, value))
            {
                OnPropertyChanged(nameof(SelectedTabName));
            }
        }
    }

    public string SelectedTabName => SelectedIndex switch
    {
        0 => "Клієнти",
        1 => "Записи",
        2 => "Працівники",
        3 => "Провайдери",
        4 => "Склад",
        _ => ""
    };

    public IRelayCommand<int> ChangeTabCommand { get; }

    public MainWindowViewModel(MySqlService db)
    {
        _db = db;
        ChangeTabCommand = new RelayCommand<int>(i => SelectedIndex = i);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var version = await _db.GetVersionAsync();
        ConnectionStatus = version is not null ? $"Connected (MySQL {version})" : "Not connected";
        OnPropertyChanged(nameof(ConnectionStatus));
    }
}
