using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class ClientDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? _mainVm;
        private static readonly string HiddenPetsFilePath = Path.Combine(AppContext.BaseDirectory, "hidden_pets.json");
        private readonly HashSet<int> _hiddenPetIds = LoadHiddenPetIds();
        private readonly DispatcherTimer _appointmentsRefreshTimer;

        [ObservableProperty] private Client _selectedClient = null!;
        [ObservableProperty] private string? _hintMessage;
        [ObservableProperty] private string? _loadError;

        public ObservableCollection<PetCardRow> PetCards { get; } = new();
        public ObservableCollection<AppointmentRow> Appointments { get; } = new();
        public ObservableCollection<ClientAppointmentGroupRow> AppointmentGroups { get; } = new();
        public ObservableCollection<BillRow> Bills { get; } = new();
        public ObservableCollection<PetType> PetTypeOptions { get; } = new();

        [ObservableProperty] private bool _isAddPetOpen;
        [ObservableProperty] private AddPetViewModel? _addPetForm;
        [ObservableProperty] private bool _isAddAppointmentOpen;
        [ObservableProperty] private AddAppointmentViewModel? _addAppointmentForm;
        [ObservableProperty] private bool _isAddBillOpen;
        [ObservableProperty] private AddBillViewModel? _addBillForm;
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private string _editFirstName = "";
        [ObservableProperty] private string _editLastName = "";
        [ObservableProperty] private string _editPhone = "";
        [ObservableProperty] private string _editEmail = "";
        [ObservableProperty] private string? _editError;

        [ObservableProperty] private bool _isEditPetOpen;
        [ObservableProperty] private PetCardRow? _editPetTarget;
        [ObservableProperty] private string _editPetName = "";
        [ObservableProperty] private string? _editPetGender;
        [ObservableProperty] private PetType? _editPetType;
        [ObservableProperty] private int? _editPetBirthDay;
        [ObservableProperty] private int? _editPetBirthMonth;
        [ObservableProperty] private int? _editPetBirthYear;
        [ObservableProperty] private string? _editPetError;

        [ObservableProperty] private decimal _paidTotal;
        [ObservableProperty] private decimal _debtTotal;

        public ObservableCollection<int> BirthDays { get; } = new();
        public ObservableCollection<int> BirthMonths { get; } = new();
        public ObservableCollection<int> BirthYears { get; } = new();
        public ObservableCollection<string> Genders { get; } = new() { "male", "female" };

        public bool IsAnyModalOpen => IsAddPetOpen || IsAddAppointmentOpen || IsAddBillOpen || IsEditMode || IsEditPetOpen;
        public bool HasPendingChanges => Appointments.Any(a => a.IsDirty) || Bills.Any(b => b.IsDirty);
        public bool HasAppointments => AppointmentGroups.Count > 0;

        public int PetsCount => PetCards.Count;
        public int AppointmentsCount => Appointments.Count;
        public string PaidTotalText => $"{PaidTotal:0.00} ₴";
        public string DebtTotalText => $"{DebtTotal:0.00} ₴";

        public ClientDetailsViewModel(Client? client = null, MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            SelectedClient = client ?? new Client();
            SeedDateOptions();
            _appointmentsRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _appointmentsRefreshTimer.Tick += (_, _) => UpdateAppointmentGroups();
            _appointmentsRefreshTimer.Start();

            if (SelectedClient.Id > 0)
            {
                _ = InitializeAsync();
            }
        }

        partial void OnIsAddPetOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsAddAppointmentOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsAddBillOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsEditModeChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsEditPetOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnPaidTotalChanged(decimal value) => OnPropertyChanged(nameof(PaidTotalText));
        partial void OnDebtTotalChanged(decimal value) => OnPropertyChanged(nameof(DebtTotalText));

        private void SeedDateOptions()
        {
            BirthDays.Clear();
            for (var i = 1; i <= 31; i++) BirthDays.Add(i);

            BirthMonths.Clear();
            for (var i = 1; i <= 12; i++) BirthMonths.Add(i);

            BirthYears.Clear();
            for (var year = DateTime.Now.Year; year >= 1950; year--) BirthYears.Add(year);
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
            if (_mainVm == null) return;
            _mainVm.CurrentViewModel = new ClientsViewModel(_mainVm);
        }

        public async Task LoadAllAsync()
        {
            await LoadPetTypesAsync();
            await LoadPetsAsync();
            await LoadAppointmentsAsync();
            await LoadBillsAsync();
            RecalculateFinancialSummary();
            RaiseDashboardCounters();
        }

        private async Task LoadPetTypesAsync()
        {
            using var db = new VetpetContext();
            var list = await db.PetTypes.OrderBy(t => t.Species).ThenBy(t => t.Breed).AsNoTracking().ToListAsync();
            PetTypeOptions.Clear();
            foreach (var item in list) PetTypeOptions.Add(item);
        }

        public async Task LoadPetsAsync()
        {
            using var db = new VetpetContext();
            var pets = await db.Pets
                .Where(p => p.ClientId == SelectedClient.Id)
                .Include(p => p.PetType)
                .AsNoTracking()
                .ToListAsync();

            var lastVisits = await db.Appointments
                .Where(a => a.ClientId == SelectedClient.Id)
                .GroupBy(a => a.PetId)
                .Select(g => new { PetId = g.Key, LastVisit = g.Max(x => x.Date) })
                .ToListAsync();

            var visitMap = lastVisits.ToDictionary(x => x.PetId, x => x.LastVisit);

            PetCards.Clear();
            foreach (var pet in pets.OrderBy(p => p.Name))
            {
                if (_hiddenPetIds.Contains(pet.Id))
                {
                    continue;
                }

                visitMap.TryGetValue(pet.Id, out var lastVisitDate);
                PetCards.Add(new PetCardRow(pet, lastVisitDate == default ? null : lastVisitDate));
            }

            RaiseDashboardCounters();
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
            foreach (var appointment in list)
            {
                var row = new AppointmentRow(appointment);
                row.PropertyChanged += OnDashboardRowPropertyChanged;
                Appointments.Add(row);
            }

            UpdateAppointmentGroups();
            RaiseDashboardCounters();
            OnPropertyChanged(nameof(HasPendingChanges));
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
            foreach (var bill in list)
            {
                var row = new BillRow(bill);
                row.PropertyChanged += OnDashboardRowPropertyChanged;
                Bills.Add(row);
            }

            RecalculateFinancialSummary();
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        private void RecalculateFinancialSummary()
        {
            PaidTotal = Bills.Where(b => b.SelectedPaid == "оплачено").Sum(b => b.Source.TotalAmount);
            DebtTotal = Bills.Where(b => b.SelectedPaid != "оплачено").Sum(b => b.Source.TotalAmount);
        }

        private void RaiseDashboardCounters()
        {
            OnPropertyChanged(nameof(PetsCount));
            OnPropertyChanged(nameof(AppointmentsCount));
        }

        private void UpdateAppointmentGroups()
        {
            var grouped = Appointments
                .GroupBy(GetAppointmentGroupKey)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Source.Date).ToList());

            AppointmentGroups.Clear();
            AddAppointmentGroup(grouped, AppointmentGroupKey.Today, "Сьогодні", "#4A7C59");
            AddAppointmentGroup(grouped, AppointmentGroupKey.Tomorrow, "Завтра", "#7C6E4A");
            AddAppointmentGroup(grouped, AppointmentGroupKey.Future, "Майбутні", "#4A6A7C");
            AddAppointmentGroup(grouped, AppointmentGroupKey.Past, "Минулі", "#6A6A6A");
            AddAppointmentGroup(grouped, AppointmentGroupKey.Cancelled, "Скасовані", "#7C4A4A");

            OnPropertyChanged(nameof(HasAppointments));
        }

        private void AddAppointmentGroup(
            IReadOnlyDictionary<AppointmentGroupKey, List<AppointmentRow>> grouped,
            AppointmentGroupKey key,
            string title,
            string markerColor)
        {
            if (!grouped.TryGetValue(key, out var items) || items.Count == 0)
            {
                return;
            }

            AppointmentGroups.Add(new ClientAppointmentGroupRow(title, markerColor, items));
        }

        private static AppointmentGroupKey GetAppointmentGroupKey(AppointmentRow row)
        {
            if (row.IsCancelled)
            {
                return AppointmentGroupKey.Cancelled;
            }

            var now = DateTime.Now;
            var todayStart = now.Date;
            var tomorrowStart = todayStart.AddDays(1);
            var dayAfterTomorrowStart = todayStart.AddDays(2);
            var date = row.Source.Date;

            if (date < now)
            {
                return AppointmentGroupKey.Past;
            }

            if (date >= todayStart && date < tomorrowStart)
            {
                return AppointmentGroupKey.Today;
            }

            if (date >= tomorrowStart && date < dayAfterTomorrowStart)
            {
                return AppointmentGroupKey.Tomorrow;
            }

            return AppointmentGroupKey.Future;
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

        [RelayCommand]
        private void OpenEditPet(PetCardRow? row)
        {
            if (row == null) return;

            EditPetTarget = row;
            EditPetName = row.Source.Name;
            EditPetGender = row.Source.Gender;
            EditPetType = PetTypeOptions.FirstOrDefault(t => t.Id == row.Source.PetTypeId);
            EditPetBirthDay = row.Source.BirthDate?.Day;
            EditPetBirthMonth = row.Source.BirthDate?.Month;
            EditPetBirthYear = row.Source.BirthDate?.Year;
            EditPetError = null;
            IsEditPetOpen = true;
        }

        [RelayCommand]
        private void CancelEditPet()
        {
            IsEditPetOpen = false;
            EditPetError = null;
        }

        [RelayCommand]
        private async Task SaveEditPet()
        {
            if (EditPetTarget == null) return;

            if (string.IsNullOrWhiteSpace(EditPetName))
            {
                EditPetError = "Кличка є обов'язковою";
                return;
            }

            if (EditPetType == null)
            {
                EditPetError = "Оберіть вид тварини";
                return;
            }

            DateOnly? parsedDate = null;
            if (EditPetBirthDay.HasValue || EditPetBirthMonth.HasValue || EditPetBirthYear.HasValue)
            {
                if (!EditPetBirthDay.HasValue || !EditPetBirthMonth.HasValue || !EditPetBirthYear.HasValue)
                {
                    EditPetError = "Оберіть повну дату або очистіть всі поля дати";
                    return;
                }

                if (!DateOnly.TryParse($"{EditPetBirthYear:0000}-{EditPetBirthMonth:00}-{EditPetBirthDay:00}", out var date))
                {
                    EditPetError = "Невірна дата народження";
                    return;
                }

                parsedDate = date;
            }

            using var db = new VetpetContext();
            var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == EditPetTarget.Source.Id && p.ClientId == SelectedClient.Id);
            if (pet == null)
            {
                EditPetError = "Тварину не знайдено";
                return;
            }

            pet.Name = EditPetName.Trim();
            pet.Gender = string.IsNullOrWhiteSpace(EditPetGender) ? null : EditPetGender;
            pet.PetTypeId = EditPetType.Id;
            pet.BirthDate = parsedDate;

            await db.SaveChangesAsync();
            IsEditPetOpen = false;
            await LoadPetsAsync();
        }

        [RelayCommand]
        private async Task DeletePet(PetCardRow? row)
        {
            if (row == null) return;

            try
            {
                _hiddenPetIds.Add(row.Source.Id);
                SaveHiddenPetIds(_hiddenPetIds);
                await LoadPetsAsync();
            }
            catch
            {
                HintMessage = "Не вдалося приховати тварину.";
            }
        }

        [RelayCommand]
        private async Task SaveDashboardChanges()
        {
            if (!HasPendingChanges) return;

            using var db = new VetpetContext();

            foreach (var appointmentRow in Appointments.Where(a => a.IsDirty))
            {
                var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentRow.Source.Id && a.ClientId == SelectedClient.Id);
                if (appointment != null)
                {
                    appointment.Status = appointmentRow.SelectedStatus;
                }
            }

            foreach (var billRow in Bills.Where(b => b.IsDirty))
            {
                var bill = await db.Bills.Include(b => b.Appointment).FirstOrDefaultAsync(b => b.Id == billRow.Source.Id && b.Appointment.ClientId == SelectedClient.Id);
                if (bill != null)
                {
                    bill.Paid = billRow.SelectedPaid;
                    bill.PaymentMethod = string.IsNullOrWhiteSpace(billRow.SelectedPaymentMethod) ? null : billRow.SelectedPaymentMethod;
                }
            }

            await db.SaveChangesAsync();

            foreach (var appointmentRow in Appointments.Where(a => a.IsDirty))
            {
                appointmentRow.MarkPersisted();
            }

            foreach (var billRow in Bills.Where(b => b.IsDirty))
            {
                billRow.MarkPersisted();
            }

            RecalculateFinancialSummary();
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        private void OnDashboardRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BillRow.SelectedPaid) || e.PropertyName == nameof(BillRow.SelectedPaymentMethod))
            {
                RecalculateFinancialSummary();
            }

            if (e.PropertyName == nameof(AppointmentRow.SelectedStatus))
            {
                UpdateAppointmentGroups();
            }

            if (e.PropertyName == nameof(AppointmentRow.IsDirty) ||
                e.PropertyName == nameof(BillRow.IsDirty) ||
                e.PropertyName == nameof(AppointmentRow.SelectedStatus) ||
                e.PropertyName == nameof(BillRow.SelectedPaid) ||
                e.PropertyName == nameof(BillRow.SelectedPaymentMethod))
            {
                OnPropertyChanged(nameof(HasPendingChanges));
            }
        }

        private static HashSet<int> LoadHiddenPetIds()
        {
            try
            {
                if (!File.Exists(HiddenPetsFilePath)) return new HashSet<int>();
                var json = File.ReadAllText(HiddenPetsFilePath);
                var list = JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
                return list.ToHashSet();
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        private static void SaveHiddenPetIds(HashSet<int> ids)
        {
            try
            {
                var json = JsonSerializer.Serialize(ids.OrderBy(x => x).ToList());
                File.WriteAllText(HiddenPetsFilePath, json);
            }
            catch
            {
                // ignore persistence failures
            }
        }

        private enum AppointmentGroupKey
        {
            Today,
            Tomorrow,
            Future,
            Past,
            Cancelled
        }
    }

    public sealed class PetCardRow
    {
        public Pet Source { get; }
        public DateTime? LastVisitDate { get; }

        public string Icon => Source.PetType?.Species?.ToLowerInvariant() switch
        {
            "кіт" => "🐱",
            "собака" => "🐶",
            "папуга" => "🦜",
            "гризун" => "🐹",
            _ => "🐾"
        };

        public string Name => Source.Name;
        public string Breed => string.IsNullOrWhiteSpace(Source.PetType?.Breed) ? "Без породи" : Source.PetType.Breed;
        public string Species => Source.PetType?.Species ?? "—";
        public string SpeciesAndBreed => $"{Species} • {Breed}";
        public string Gender => string.IsNullOrWhiteSpace(Source.Gender) ? "невідомо" : Source.Gender;
        public string BirthDate => Source.BirthDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "—";
        public string LastVisit => LastVisitDate?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "немає";

        public PetCardRow(Pet source, DateTime? lastVisitDate)
        {
            Source = source;
            LastVisitDate = lastVisitDate;
        }
    }

    public sealed class ClientAppointmentGroupRow
    {
        public string Title { get; }
        public string MarkerColor { get; }
        public ObservableCollection<AppointmentRow> Items { get; } = new();

        public ClientAppointmentGroupRow(string title, string markerColor, IEnumerable<AppointmentRow> items)
        {
            Title = title;
            MarkerColor = markerColor;
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
    }

    public partial class AppointmentRow : ObservableObject
    {
        public Appointment Source { get; }

        [ObservableProperty] private string _selectedStatus;

        public ObservableCollection<string> StatusOptions { get; } = new() { "заплановано", "виконано", "скасовано" };

        public string DateShort => Source.Date.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        public string PetName => Source.Pet?.Name ?? "—";
        public string StaffName => Source.Staff != null ? $"{Source.Staff.LastName} {Source.Staff.FirstName}" : "—";
        public string ProcedureName => Source.Service?.Name ?? "Без послуги";
        public string Description => string.IsNullOrWhiteSpace(Source.Description) ? "—" : Source.Description!;
        public bool IsCancelled => string.Equals(SelectedStatus?.Trim(), "скасовано", StringComparison.OrdinalIgnoreCase);

        public string StatusColor => SelectedStatus switch
        {
            "виконано" => "#4A7C59",
            "скасовано" => "#7C4A4A",
            _ => "#7C6E4A"
        };

        public string DateMarkerColor
        {
            get
            {
                if (IsCancelled)
                {
                    return "#7C4A4A";
                }

                var now = DateTime.Now;
                var todayStart = now.Date;
                var tomorrowStart = todayStart.AddDays(1);
                var dayAfterTomorrowStart = todayStart.AddDays(2);
                var date = Source.Date;

                if (date < now)
                {
                    return "#6A6A6A";
                }

                if (date >= todayStart && date < tomorrowStart)
                {
                    return "#4A7C59";
                }

                if (date >= tomorrowStart && date < dayAfterTomorrowStart)
                {
                    return "#7C6E4A";
                }

                return "#4A6A7C";
            }
        }

        public bool IsDirty => !string.Equals(Source.Status ?? "заплановано", SelectedStatus, StringComparison.Ordinal);

        public AppointmentRow(Appointment appointment)
        {
            Source = appointment;
            _selectedStatus = string.IsNullOrWhiteSpace(appointment.Status) ? "заплановано" : appointment.Status!;
        }

        partial void OnSelectedStatusChanged(string value)
        {
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsCancelled));
            OnPropertyChanged(nameof(DateMarkerColor));
            OnPropertyChanged(nameof(IsDirty));
        }

        public void MarkPersisted()
        {
            Source.Status = SelectedStatus;
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    public partial class BillRow : ObservableObject
    {
        public Bill Source { get; }

        [ObservableProperty] private string _selectedPaid;
        [ObservableProperty] private string? _selectedPaymentMethod;

        public ObservableCollection<string> PaidOptions { get; } = new() { "без оплати", "оплачено" };
        public ObservableCollection<string> PaymentMethodOptions { get; } = new() { "готівка", "карта" };

        public string Date => Source.Date?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "—";
        public string Amount => $"{Source.TotalAmount:0.00} ₴";

        public string PaidColor => SelectedPaid == "оплачено" ? "#4A7C59" : "#7C4A4A";
        public string PaymentBadgeColor => SelectedPaymentMethod switch
        {
            "карта" => "#4A6A7C",
            "готівка" => "#7C6E4A",
            _ => "#555555"
        };

        public bool IsDirty =>
            !string.Equals(Source.Paid ?? "без оплати", SelectedPaid, StringComparison.Ordinal) ||
            !string.Equals(Source.PaymentMethod ?? string.Empty, SelectedPaymentMethod ?? string.Empty, StringComparison.Ordinal);

        public BillRow(Bill bill)
        {
            Source = bill;
            _selectedPaid = string.IsNullOrWhiteSpace(bill.Paid) ? "без оплати" : bill.Paid!;
            _selectedPaymentMethod = string.IsNullOrWhiteSpace(bill.PaymentMethod) ? null : bill.PaymentMethod;
        }

        partial void OnSelectedPaidChanged(string value)
        {
            OnPropertyChanged(nameof(PaidColor));
            OnPropertyChanged(nameof(IsDirty));
        }

        partial void OnSelectedPaymentMethodChanged(string? value)
        {
            OnPropertyChanged(nameof(PaymentBadgeColor));
            OnPropertyChanged(nameof(IsDirty));
        }

        public void MarkPersisted()
        {
            Source.Paid = SelectedPaid;
            Source.PaymentMethod = SelectedPaymentMethod;
            OnPropertyChanged(nameof(IsDirty));
        }
    }
}
