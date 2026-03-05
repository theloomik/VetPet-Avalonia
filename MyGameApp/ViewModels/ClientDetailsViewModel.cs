using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class ClientDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? _mainVm;

        [ObservableProperty] private Client _selectedClient = null!;
        [ObservableProperty] private int _activeTab = 0;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string? _hintMessage;
        [ObservableProperty] private string? _loadError;

        public ObservableCollection<Pet> Pets { get; } = new();
        public ObservableCollection<AppointmentRow> Appointments { get; } = new();
        public ObservableCollection<BillRow> Bills { get; } = new();
        private readonly ObservableCollection<Pet> _allPets = new();
        private readonly ObservableCollection<AppointmentRow> _allAppointments = new();
        private readonly ObservableCollection<BillRow> _allBills = new();

        [ObservableProperty] private bool _isAddPetOpen = false;
        [ObservableProperty] private AddPetViewModel? _addPetForm;
        [ObservableProperty] private bool _isAddAppointmentOpen = false;
        [ObservableProperty] private AddAppointmentViewModel? _addAppointmentForm;
        [ObservableProperty] private bool _isAddBillOpen = false;
        [ObservableProperty] private AddBillViewModel? _addBillForm;
        [ObservableProperty] private bool _isEditMode = false;
        [ObservableProperty] private string _editFirstName = "";
        [ObservableProperty] private string _editLastName = "";
        [ObservableProperty] private string _editPhone = "";
        [ObservableProperty] private string _editEmail = "";
        [ObservableProperty] private string? _editError;

        public bool IsAnyModalOpen => IsAddPetOpen || IsAddAppointmentOpen || IsAddBillOpen || IsEditMode;

        public ClientDetailsViewModel(Client? client = null, MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            SelectedClient = client ?? new Client();

            if (SelectedClient.Id > 0)
            {
                _ = InitializeAsync();
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                LoadError = null;
                await LoadAllAsync();
            }
            catch (Exception ex)
            {
                LoadError = $"Не вдалося завантажити дані клієнта: {ex.Message}";
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            if (_mainVm == null)
                return;

            _mainVm.CurrentViewModel = new ClientsViewModel(_mainVm);
        }

        [RelayCommand]
        private void SetTab(int tab) => ActiveTab = tab;

        public bool IsPetsTab => ActiveTab == 0;
        public bool IsAppointmentsTab => ActiveTab == 1;
        public bool IsBillsTab => ActiveTab == 2;

        public string SearchWatermark => ActiveTab == 0
            ? "Пошук за кличкою тварини"
            : "Пошук за датою (ДД.ММ.РРРР)";

        partial void OnActiveTabChanged(int value)
        {
            OnPropertyChanged(nameof(IsPetsTab));
            OnPropertyChanged(nameof(IsAppointmentsTab));
            OnPropertyChanged(nameof(IsBillsTab));
            OnPropertyChanged(nameof(SearchWatermark));
            SearchText = string.Empty;
            HintMessage = null;
            ApplyFilter();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnIsAddPetOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsAddAppointmentOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsAddBillOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsEditModeChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));

        public async Task LoadAllAsync()
        {
            await LoadPetsAsync();
            await LoadAppointmentsAsync();
            await LoadBillsAsync();
        }

        public async Task LoadPetsAsync()
        {
            using var db = new VetpetContext();
            var list = await db.Pets
                .Where(p => p.ClientId == SelectedClient.Id)
                .Include(p => p.PetType)
                .AsNoTracking()
                .ToListAsync();
            _allPets.Clear();
            foreach (var p in list) _allPets.Add(p);
            ApplyFilter();
        }

        public async Task LoadAppointmentsAsync()
        {
            using var db = new VetpetContext();
            var list = await db.Appointments
                .Where(a => a.ClientId == SelectedClient.Id)
                .Include(a => a.Pet)
                .Include(a => a.Staff)
                .Include(a => a.Service)
                .OrderByDescending(a => a.Date)
                .AsNoTracking()
                .ToListAsync();
            _allAppointments.Clear();
            foreach (var a in list) _allAppointments.Add(new AppointmentRow(a));
            ApplyFilter();
        }

        public async Task LoadBillsAsync()
        {
            using var db = new VetpetContext();
            var list = await db.Bills
                .Include(b => b.Appointment)
                .Where(b => b.Appointment != null && b.Appointment.ClientId == SelectedClient.Id)
                .OrderByDescending(b => b.Date)
                .AsNoTracking()
                .ToListAsync();
            _allBills.Clear();
            foreach (var b in list) _allBills.Add(new BillRow(b));
            ApplyFilter();
        }

        [RelayCommand]
        private void OpenAddPet()
        {
            AddPetForm = new AddPetViewModel(this);
            IsAddPetOpen = true;
        }

        [RelayCommand]
        private void OpenAddAppointment()
        {
            AddAppointmentForm = new AddAppointmentViewModel(this);
            IsAddAppointmentOpen = true;
        }

        [RelayCommand]
        private void OpenAddBill()
        {
            AddBillForm = new AddBillViewModel(this);
            IsAddBillOpen = true;
        }

        [RelayCommand]
        private void AddByTab()
        {
            HintMessage = null;

            if (ActiveTab == 0)
            {
                OpenAddPet();
                return;
            }

            if (ActiveTab == 1)
            {
                OpenAddAppointment();
                return;
            }

            OpenAddBill();
        }

        [RelayCommand]
        private void StartEdit()
        {
            EditFirstName = SelectedClient.FirstName ?? "";
            EditLastName = SelectedClient.LastName ?? "";
            EditPhone = SelectedClient.Phone ?? "";
            EditEmail = SelectedClient.Email ?? "";
            EditError = null;
            IsEditMode = true;
        }

        [RelayCommand]
        private void CancelEdit() => IsEditMode = false;

        [RelayCommand]
        private async Task SaveEdit()
        {
            if (string.IsNullOrWhiteSpace(EditLastName) || string.IsNullOrWhiteSpace(EditFirstName) || string.IsNullOrWhiteSpace(EditPhone))
            {
                EditError = "Прізвище, ім'я та телефон є обов'язковими";
                return;
            }

            using var db = new VetpetContext();
            var client = await db.Clients.FindAsync(SelectedClient.Id);
            if (client == null) return;

            client.FirstName = EditFirstName.Trim();
            client.LastName = EditLastName.Trim();
            client.Phone = EditPhone.Trim();
            client.Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim();
            await db.SaveChangesAsync();

            SelectedClient.FirstName = client.FirstName;
            SelectedClient.LastName = client.LastName;
            SelectedClient.Phone = client.Phone;
            SelectedClient.Email = client.Email;
            OnPropertyChanged(nameof(SelectedClient));
            IsEditMode = false;
        }

        private void ApplyFilter()
        {
            var query = SearchText?.Trim() ?? string.Empty;
            var dateOnly = query.Length > 10 ? query[..10] : query;

            Pets.Clear();
            foreach (var pet in _allPets.Where(p => string.IsNullOrWhiteSpace(query) || (p.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)))
            {
                Pets.Add(pet);
            }

            Appointments.Clear();
            foreach (var appointment in _allAppointments.Where(a => string.IsNullOrWhiteSpace(query) || a.Source.Date.ToString("dd.MM.yyyy").Contains(dateOnly)))
            {
                Appointments.Add(appointment);
            }

            Bills.Clear();
            foreach (var bill in _allBills.Where(b => string.IsNullOrWhiteSpace(query) || (b.Source.Date?.ToString("dd.MM.yyyy").Contains(dateOnly) ?? false)))
            {
                Bills.Add(bill);
            }
        }
    }

    public class AppointmentRow
    {
        public Appointment Source { get; }
        public string Date => Source.Date.ToString("dd.MM.yyyy HH:mm");
        public string PetName => Source.Pet?.Name ?? "—";
        public string StaffName => Source.Staff != null ? $"{Source.Staff.LastName} {Source.Staff.FirstName}" : "—";
        public string ServiceName => Source.Service?.Name ?? "—";
        public string Status => Source.Status ?? "—";
        public string StatusColor => Source.Status switch
        {
            "виконано" => "#4A7C59",
            "скасовано" => "#7C4A4A",
            _ => "#7C6E4A"
        };

        public AppointmentRow(Appointment a) => Source = a;
    }

    public class BillRow
    {
        public Bill Source { get; }
        public string Date => Source.Date?.ToString("dd.MM.yyyy HH:mm") ?? "—";
        public string Amount => $"{Source.TotalAmount:0.00} ₴";
        public string Paid => Source.Paid ?? "без оплати";
        public string PaidColor => Source.Paid == "оплачено" ? "#4A7C59" : "#7C4A4A";

        public BillRow(Bill b) => Source = b;
    }
}
