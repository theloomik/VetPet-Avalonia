using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Важливо для RelayCommand
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // === ПРИВАТНІ СПИСКИ (Повні дані з БД) ===
        private List<Client> _allClients = new();
        private List<Appointment> _allAppointments = new();
        private List<Staff> _allStaff = new();
        private List<Provider> _allProviders = new();
        private List<Stock> _allStock = new();

        // === ПУБЛІЧНІ КОЛЕКЦІЇ (Для UI) ===
        public ObservableCollection<Client> Clients { get; } = new();
        public ObservableCollection<Appointment> Appointments { get; } = new();
        public ObservableCollection<Staff> StaffMembers { get; } = new();
        public ObservableCollection<Provider> Providers { get; } = new();
        public ObservableCollection<Stock> Stocks { get; } = new();

        // === СТАН ВІКНА ТА ВКЛАДОК ===
        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (SetProperty(ref _selectedIndex, value))
                {
                    // Оновлюємо видимість вкладок
                    OnPropertyChanged(nameof(SelectedTabName));
                    OnPropertyChanged(nameof(IsClients));
                    OnPropertyChanged(nameof(IsAppointments));
                    OnPropertyChanged(nameof(IsStaff));
                    OnPropertyChanged(nameof(IsProviders));
                    OnPropertyChanged(nameof(IsStock));

                    // Скидаємо пошук і сортування при зміні вкладки
                    SearchText = "";
                    _isSortAscending = true;
                    SortLabel = "A ↓";
                    
                    // Оновлюємо список для нової вкладки
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

        // Властивості видимості
        public bool IsClients => SelectedIndex == 0;
        public bool IsAppointments => SelectedIndex == 1;
        public bool IsStaff => SelectedIndex == 2;
        public bool IsProviders => SelectedIndex == 3;
        public bool IsStock => SelectedIndex == 4;

        // === ПОШУК ТА СОРТУВАННЯ ===
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    UpdateList(); // Авто-оновлення при введенні тексту
                }
            }
        }

        private string _sortLabel = "A ↓";
        public string SortLabel
        {
            get => _sortLabel;
            set => SetProperty(ref _sortLabel, value);
        }

        private bool _isSortAscending = true; // true = А-Я, false = Я-А

        // Команди
        public IRelayCommand<int> ChangeTabCommand { get; }
        public IRelayCommand ToggleSortCommand { get; }

        // Конструктор
        public MainWindowViewModel()
        {
            ChangeTabCommand = new RelayCommand<int>(i => SelectedIndex = i);
            ToggleSortCommand = new RelayCommand(ToggleSort);
            
            _ = InitializeAsync();
        }

        private void ToggleSort()
        {
            _isSortAscending = !_isSortAscending;
            SortLabel = _isSortAscending ? "A ↓" : "Z ↑";
            UpdateList();
        }

        // === ГОЛОВНА ЛОГІКА ОНОВЛЕННЯ СПИСКІВ ===
        private void UpdateList()
        {
            string search = SearchText?.ToLower().Trim() ?? "";

            switch (SelectedIndex)
            {
                case 0: // Клієнти
                    var qClients = _allClients.Where(c => c.Phone != null && c.Phone.Contains(search));
                    
                    if (_isSortAscending)
                        qClients = qClients.OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
                    else
                        qClients = qClients.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName);

                    Clients.Clear();
                    foreach (var item in qClients) Clients.Add(item);
                    break;

                case 1: // Записи
                    var qAppt = _allAppointments.Where(a => a.Client?.Phone != null && a.Client.Phone.Contains(search));

                    if (_isSortAscending)
                        qAppt = qAppt.OrderBy(a => a.Date);
                    else
                        qAppt = qAppt.OrderByDescending(a => a.Date);

                    Appointments.Clear();
                    foreach (var item in qAppt) Appointments.Add(item);
                    break;

                case 2: // Працівники
                    var qStaff = _allStaff.Where(s => s.Phone != null && s.Phone.Contains(search));

                    if (_isSortAscending)
                        qStaff = qStaff.OrderBy(s => s.LastName).ThenBy(s => s.FirstName);
                    else
                        qStaff = qStaff.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName);

                    StaffMembers.Clear();
                    foreach (var item in qStaff) StaffMembers.Add(item);
                    break;

                case 3: // Провайдери
                    var qProv = _allProviders.Where(p => p.Name != null && p.Name.ToLower().Contains(search));

                    if (_isSortAscending)
                        qProv = qProv.OrderBy(p => p.Name);
                    else
                        qProv = qProv.OrderByDescending(p => p.Name);

                    Providers.Clear();
                    foreach (var item in qProv) Providers.Add(item);
                    break;

                case 4: // Склад
                    var qStock = _allStock.Where(s => s.Medicine?.Name != null && s.Medicine.Name.ToLower().Contains(search));

                    if (_isSortAscending)
                        qStock = qStock.OrderBy(s => s.Medicine.Name);
                    else
                        qStock = qStock.OrderByDescending(s => s.Medicine.Name);

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
                
                // 1. Завантажуємо Клієнтів
                var clients = await db.Clients.AsNoTracking().ToListAsync();
                _allClients = clients; 

                // 2. Завантажуємо Записи
                var appointments = await db.Appointments
                    .AsNoTracking()
                    .Include(a => a.Client)
                    .Include(a => a.Pet)
                    .ToListAsync();
                _allAppointments = appointments;

                // 3. Завантажуємо Працівників
                var staff = await db.Staff.AsNoTracking().ToListAsync();
                _allStaff = staff;

                // 4. Завантажуємо Провайдерів
                var providers = await db.Providers.AsNoTracking().ToListAsync();
                _allProviders = providers;

                // 5. Завантажуємо Склад
                var stocks = await db.Stocks.AsNoTracking().Include(s => s.Medicine).ToListAsync();
                _allStock = stocks;

                // Оновлюємо UI
                UpdateList();
            }
            catch (Exception)
            {
                // Тут можна додати логування помилки
            }
        }
    }
}