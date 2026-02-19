using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private List<Client> _allClients = new();
        private List<Appointment> _allAppointments = new();
        private List<Staff> _allStaff = new();
        private List<Provider> _allProviders = new();
        private List<Stock> _allStock = new();

        public ObservableCollection<Client> Clients { get; } = new();
        public ObservableCollection<Appointment> Appointments { get; } = new();
        public ObservableCollection<Staff> StaffMembers { get; } = new();
        public ObservableCollection<Provider> Providers { get; } = new();
        public ObservableCollection<Stock> Stocks { get; } = new();

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

                    _isSortAscending = true;
                    SortLabel = "A ↓";

                    UpdateList();
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

        public bool IsClients => SelectedIndex == 0;
        public bool IsAppointments => SelectedIndex == 1;
        public bool IsStaff => SelectedIndex == 2;
        public bool IsProviders => SelectedIndex == 3;
        public bool IsStock => SelectedIndex == 4;

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    UpdateList();
                }
            }
        }

        private string _sortLabel = "A ↓";
        public string SortLabel
        {
            get => _sortLabel;
            set => SetProperty(ref _sortLabel, value);
        }

        private bool _isSortAscending = true;

        public IRelayCommand<int> ChangeTabCommand { get; }
        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand OpenAddClientCommand { get; }
        public IRelayCommand CloseAddClientCommand { get; }

        // -------- МОДАЛКА --------

        public enum AddEntityType
        {
            None,
            Client,
            Staff,
            Appointment,
            Provider,
            Stock
        }

        private AddEntityType _currentAddType = AddEntityType.None;
        public AddEntityType CurrentAddType
        {
            get => _currentAddType;
            set
            {
                if (SetProperty(ref _currentAddType, value))
                {
                    OnPropertyChanged(nameof(IsAddOpen));
                    OnPropertyChanged(nameof(BlurRadius));
                }
            }
        }

        public bool IsAddOpen => CurrentAddType != AddEntityType.None;
        public double BlurRadius => IsAddOpen ? 8 : 0;

        // -------- КОНСТРУКТОР --------

        public MainWindowViewModel()
        {
            ChangeTabCommand = new RelayCommand<int>(i => SelectedIndex = i);
            ToggleSortCommand = new RelayCommand(ToggleSort);

            OpenAddClientCommand = new RelayCommand(() =>
            {
                switch (SelectedIndex)
                {
                    case 0:
                        CurrentAddType = AddEntityType.Client;
                        break;
                    case 1:
                        CurrentAddType = AddEntityType.Appointment;
                        break;
                    case 2:
                        CurrentAddType = AddEntityType.Staff;
                        break;
                    case 3:
                        CurrentAddType = AddEntityType.Provider;
                        break;
                    case 4:
                        CurrentAddType = AddEntityType.Stock;
                        break;
                }
            });

            CloseAddClientCommand = new RelayCommand(() =>
            {
                CurrentAddType = AddEntityType.None;
            });

            _ = InitializeAsync();
        }

        private void ToggleSort()
        {
            _isSortAscending = !_isSortAscending;
            SortLabel = _isSortAscending ? "A ↓" : "Z ↑";
            UpdateList();
        }

        private void UpdateList()
        {
            string search = SearchText?.ToLower().Trim() ?? "";

            switch (SelectedIndex)
            {
                case 0:
                    var qClients = _allClients.Where(c => c.Phone != null && c.Phone.Contains(search));
                    qClients = _isSortAscending
                        ? qClients.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                        : qClients.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName);

                    Clients.Clear();
                    foreach (var item in qClients) Clients.Add(item);
                    break;

                case 1:
                    var qAppt = _allAppointments.Where(a => a.Client?.Phone != null && a.Client.Phone.Contains(search));
                    qAppt = _isSortAscending
                        ? qAppt.OrderBy(a => a.Date)
                        : qAppt.OrderByDescending(a => a.Date);

                    Appointments.Clear();
                    foreach (var item in qAppt) Appointments.Add(item);
                    break;

                case 2:
                    var qStaff = _allStaff.Where(s => s.Phone != null && s.Phone.Contains(search));
                    qStaff = _isSortAscending
                        ? qStaff.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
                        : qStaff.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName);

                    StaffMembers.Clear();
                    foreach (var item in qStaff) StaffMembers.Add(item);
                    break;

                case 3:
                    var qProv = _allProviders.Where(p => p.Name != null && p.Name.ToLower().Contains(search));
                    qProv = _isSortAscending
                        ? qProv.OrderBy(p => p.Name)
                        : qProv.OrderByDescending(p => p.Name);

                    Providers.Clear();
                    foreach (var item in qProv) Providers.Add(item);
                    break;

                case 4:
                    var qStock = _allStock.Where(s => s.Medicine?.Name != null && s.Medicine.Name.ToLower().Contains(search));
                    qStock = _isSortAscending
                        ? qStock.OrderBy(s => s.Medicine.Name)
                        : qStock.OrderByDescending(s => s.Medicine.Name);

                    Stocks.Clear();
                    foreach (var item in qStock) Stocks.Add(item);
                    break;
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                using var db = new VetpetContext();

                _allClients = await db.Clients.AsNoTracking().ToListAsync();
                _allAppointments = await db.Appointments
                    .AsNoTracking()
                    .Include(a => a.Client)
                    .Include(a => a.Pet)
                    .ToListAsync();
                _allStaff = await db.Staff.AsNoTracking().ToListAsync();
                _allProviders = await db.Providers.AsNoTracking().ToListAsync();
                _allStock = await db.Stocks.AsNoTracking().Include(s => s.Medicine).ToListAsync();

                UpdateList();
            }
            catch
            {
                // поки без логування
            }
        }
    }
}
