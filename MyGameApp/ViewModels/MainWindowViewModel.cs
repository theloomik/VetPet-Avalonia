namespace MyGameApp.ViewModels;

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MyGameApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Hello";

    public string ConnectionStatus { get; private set; } = "Not initialized";

    private int _selectedIndex;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (SetProperty(ref _selectedIndex, value))
            {
                OnPropertyChanged(nameof(SelectedTabName));
                OnPropertyChanged(nameof(IsClients));
                OnPropertyChanged(nameof(IsAppointments));
                OnPropertyChanged(nameof(IsStaff));
                OnPropertyChanged(nameof(IsProviders));
                OnPropertyChanged(nameof(IsStock));
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

    public ObservableCollection<Client> Clients { get; } = new ObservableCollection<Client>();
    public ObservableCollection<Appointment> Appointments { get; } = new ObservableCollection<Appointment>();
    public ObservableCollection<Staff> StaffMembers { get; } = new ObservableCollection<Staff>();
    public ObservableCollection<Provider> Providers { get; } = new ObservableCollection<Provider>();
    public ObservableCollection<Stock> Stocks { get; } = new ObservableCollection<Stock>();

    public bool IsClients => SelectedIndex == 0;
    public bool IsAppointments => SelectedIndex == 1;
    public bool IsStaff => SelectedIndex == 2;
    public bool IsProviders => SelectedIndex == 3;
    public bool IsStock => SelectedIndex == 4;

    public IRelayCommand<int> ChangeTabCommand { get; }

    public MainWindowViewModel()
    {
        ChangeTabCommand = new RelayCommand<int>(i => SelectedIndex = i);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // var version = await _db.GetVersionAsync();
        // ConnectionStatus = version is not null ? $"Connected (MySQL {version})" : "Not connected";
        OnPropertyChanged(nameof(ConnectionStatus));

        // Load clients and other lists from the database
        try
        {
            using var db = new VetpetContext();
            var clients = await db.Clients.AsNoTracking().ToListAsync();
            Clients.Clear();
            foreach (var c in clients)
                Clients.Add(c);

            var appointments = await db.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.Pet)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            Appointments.Clear();
            foreach (var a in appointments)
                Appointments.Add(a);

            var staff = await db.Staff.AsNoTracking().ToListAsync();
            StaffMembers.Clear();
            foreach (var s in staff)
                StaffMembers.Add(s);

            var providers = await db.Providers.AsNoTracking().ToListAsync();
            Providers.Clear();
            foreach (var p in providers)
                Providers.Add(p);

            var stocks = await db.Stocks.AsNoTracking().Include(s => s.Medicine).ToListAsync();
            Stocks.Clear();
            foreach (var st in stocks)
                Stocks.Add(st);
        }
        catch
        {
            // ignore DB errors for now; app can still run without DB
        }
    }
}
