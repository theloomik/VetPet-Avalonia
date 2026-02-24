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
        private readonly MainWindowViewModel _mainVm;

        [ObservableProperty] private Client _selectedClient = null!;
        [ObservableProperty] private int _activeTab = 0;
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private string? _actionInfo;

        public ObservableCollection<Pet> Pets { get; } = new();
        public ObservableCollection<AppointmentRow> Appointments { get; } = new();
        public ObservableCollection<BillRow> Bills { get; } = new();

        public ObservableCollection<Pet> FilteredPets { get; } = new();
        public ObservableCollection<AppointmentRow> FilteredAppointments { get; } = new();
        public ObservableCollection<BillRow> FilteredBills { get; } = new();

        [ObservableProperty] private bool _isAddPetOpen = false;
        [ObservableProperty] private AddPetViewModel? _addPetForm;
        [ObservableProperty] private bool _isEditMode = false;
        [ObservableProperty] private string _editFirstName = "";
        [ObservableProperty] private string _editLastName = "";
        [ObservableProperty] private string _editPhone = "";
        [ObservableProperty] private string _editEmail = "";
        [ObservableProperty] private string? _editError;

        public string SearchHint => ActiveTab switch
        {
            0 => "Пошук тварини за кличкою",
            1 => "Пошук запису за датою (дд.мм.рррр)",
            2 => "Пошук рахунку за датою (дд.мм.рррр)",
            _ => "Пошук"
        };

        public string AddButtonText => ActiveTab switch
        {
            0 => "+ Тварина",
            1 => "+ Запис",
            2 => "+ Рахунок",
            _ => "+ Додати"
        };

        public ClientDetailsViewModel(Client? client = null, MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));
            SelectedClient = client ?? new Client();

            if (SelectedClient.Id > 0)
            {
                _ = LoadAllAsync();
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();

        partial void OnActiveTabChanged(int value)
        {
            ActionInfo = null;
            OnPropertyChanged(nameof(SearchHint));
            OnPropertyChanged(nameof(AddButtonText));
            ApplyFilters();
        }

        [RelayCommand]
        private void GoBack() => _mainVm.CurrentViewModel = new ClientsViewModel(_mainVm);

        [RelayCommand]
        private void SetTab(int tab) => ActiveTab = tab;

        [RelayCommand]
        private void AddByActiveTab()
        {
            ActionInfo = null;
            if (ActiveTab == 0)
            {
                OpenAddPet();
                return;
            }

            ActionInfo = ActiveTab == 1
                ? "Додавання запису буде в наступному оновленні."
                : "Створення рахунку буде в наступному оновленні.";
        }

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
            Pets.Clear();
            foreach (var p in list) Pets.Add(p);
            ApplyFilters();
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
            Appointments.Clear();
            foreach (var a in list) Appointments.Add(new AppointmentRow(a));
            ApplyFilters();
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
            Bills.Clear();
            foreach (var b in list) Bills.Add(new BillRow(b));
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var q = SearchText?.Trim() ?? "";

            var pets = string.IsNullOrWhiteSpace(q)
                ? Pets
                : new ObservableCollection<Pet>(Pets.Where(p => (p.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)));
            Sync(FilteredPets, pets);

            var appointments = string.IsNullOrWhiteSpace(q)
                ? Appointments
                : new ObservableCollection<AppointmentRow>(Appointments.Where(a => a.Source.Date.ToString("dd.MM.yyyy").Contains(q, StringComparison.OrdinalIgnoreCase)));
            Sync(FilteredAppointments, appointments);

            var bills = string.IsNullOrWhiteSpace(q)
                ? Bills
                : new ObservableCollection<BillRow>(Bills.Where(b => (b.Source.Date?.ToString("dd.MM.yyyy") ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)));
            Sync(FilteredBills, bills);
        }

        private static void Sync<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
                target.Add(item);
        }

        [RelayCommand]
        private void OpenAddPet()
        {
            AddPetForm = new AddPetViewModel(this);
            IsAddPetOpen = true;
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
