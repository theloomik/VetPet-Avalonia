using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class QuickAppointmentViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _parent;

        [ObservableProperty] private string _clientPhone = "";
        [ObservableProperty] private Client? _selectedClient;
        [ObservableProperty] private Pet? _selectedPet;
        [ObservableProperty] private Staff? _selectedStaff;
        [ObservableProperty] private Service? _selectedService;
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private string _selectedStatus = "заплановано";
        [ObservableProperty] private string? _errorMessage;

        [ObservableProperty] private bool _isAddClientOpen;
        [ObservableProperty] private string _newClientFirstName = "";
        [ObservableProperty] private string _newClientLastName = "";
        [ObservableProperty] private string _newClientPhone = "";
        [ObservableProperty] private string _newClientEmail = "";
        [ObservableProperty] private string? _newClientErrorMessage;

        [ObservableProperty] private bool _isAddPetOpen;
        [ObservableProperty] private string _newPetName = "";
        [ObservableProperty] private PetType? _newPetType;
        [ObservableProperty] private string? _newPetGender = "male";
        [ObservableProperty] private int? _newPetBirthDay;
        [ObservableProperty] private MonthOption? _newPetBirthMonth;
        [ObservableProperty] private int? _newPetBirthYear;
        [ObservableProperty] private string? _newPetErrorMessage;

        [ObservableProperty] private int? _selectedDay;
        [ObservableProperty] private MonthOption? _selectedMonth;
        [ObservableProperty] private int? _selectedYear;
        [ObservableProperty] private int? _selectedHour;
        [ObservableProperty] private int? _selectedMinute;

        public ObservableCollection<Client> ClientMatches { get; } = new();
        public ObservableCollection<Pet> PetOptions { get; } = new();
        public ObservableCollection<Staff> StaffOptions { get; } = new();
        public ObservableCollection<Service> ServiceOptions { get; } = new();
        public ObservableCollection<PetType> PetTypeOptions { get; } = new();

        public ObservableCollection<string> StatusOptions { get; } = new() { "заплановано", "виконано", "скасовано" };
        public ObservableCollection<string> PetGenderOptions { get; } = new() { "male", "female" };
        public ObservableCollection<int> DayOptions { get; } = new();
        public ObservableCollection<MonthOption> MonthOptions { get; } = new();
        public ObservableCollection<int> YearOptions { get; } = new();
        public ObservableCollection<int> HourOptions { get; } = new();
        public ObservableCollection<int> MinuteOptions { get; } = new();

        public bool CanCreatePet => SelectedClient != null;
        public bool IsAnyModalOpen => IsAddClientOpen || IsAddPetOpen;

        public QuickAppointmentViewModel(MainWindowViewModel parent)
        {
            _parent = parent;
            SeedDateOptions();
            _ = LoadInitialOptionsAsync();
        }

        partial void OnClientPhoneChanged(string value)
        {
            _ = LoadClientMatchesAsync();
        }

        partial void OnSelectedClientChanged(Client? value)
        {
            OnPropertyChanged(nameof(CanCreatePet));
            _ = LoadPetsForClientAsync(value?.Id);
        }

        partial void OnIsAddClientOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsAddPetOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));

        private void SeedDateOptions()
        {
            DayOptions.Clear();
            for (var i = 1; i <= 31; i++)
            {
                DayOptions.Add(i);
            }

            MonthOptions.Clear();
            MonthOptions.Add(new MonthOption(1, "Січень"));
            MonthOptions.Add(new MonthOption(2, "Лютий"));
            MonthOptions.Add(new MonthOption(3, "Березень"));
            MonthOptions.Add(new MonthOption(4, "Квітень"));
            MonthOptions.Add(new MonthOption(5, "Травень"));
            MonthOptions.Add(new MonthOption(6, "Червень"));
            MonthOptions.Add(new MonthOption(7, "Липень"));
            MonthOptions.Add(new MonthOption(8, "Серпень"));
            MonthOptions.Add(new MonthOption(9, "Вересень"));
            MonthOptions.Add(new MonthOption(10, "Жовтень"));
            MonthOptions.Add(new MonthOption(11, "Листопад"));
            MonthOptions.Add(new MonthOption(12, "Грудень"));

            YearOptions.Clear();
            var currentYear = DateTime.Now.Year;
            for (var year = currentYear + 1; year >= 2024; year--)
            {
                YearOptions.Add(year);
            }

            HourOptions.Clear();
            for (var hour = 0; hour < 24; hour++)
            {
                HourOptions.Add(hour);
            }

            MinuteOptions.Clear();
            for (var minute = 0; minute < 60; minute += 5)
            {
                MinuteOptions.Add(minute);
            }

            var initial = DateTime.Now.AddHours(1);
            SelectedDay = initial.Day;
            SelectedMonth = MonthOptions.FirstOrDefault(m => m.Number == initial.Month);
            SelectedYear = initial.Year;
            SelectedHour = initial.Hour;
            SelectedMinute = (initial.Minute / 5) * 5;
        }

        private async Task LoadInitialOptionsAsync()
        {
            using var db = new VetpetContext();

            var staff = await db.Staff
                .Include(s => s.StaffPosition)
                .Where(s => s.WorkDays != StaffArchive.ArchivedMarker)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .AsNoTracking()
                .ToListAsync();

            StaffOptions.Clear();
            foreach (var row in staff)
            {
                StaffOptions.Add(row);
            }

            SelectedStaff = StaffOptions.FirstOrDefault();

            var services = await db.Services
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

            ServiceOptions.Clear();
            foreach (var row in services)
            {
                ServiceOptions.Add(row);
            }

            SelectedService = ServiceOptions.FirstOrDefault();

            var petTypes = await db.PetTypes
                .OrderBy(t => t.Species)
                .ThenBy(t => t.Breed)
                .AsNoTracking()
                .ToListAsync();

            PetTypeOptions.Clear();
            foreach (var row in petTypes)
            {
                PetTypeOptions.Add(row);
            }

            NewPetType = PetTypeOptions.FirstOrDefault();
        }

        private async Task LoadClientMatchesAsync()
        {
            var phoneQuery = ClientPhone.Trim();
            if (string.IsNullOrWhiteSpace(phoneQuery))
            {
                ClientMatches.Clear();
                SelectedClient = null;
                PetOptions.Clear();
                SelectedPet = null;
                return;
            }

            using var db = new VetpetContext();
            var matches = await db.Clients
                .Where(c => c.Phone.Contains(phoneQuery))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Take(20)
                .AsNoTracking()
                .ToListAsync();

            ClientMatches.Clear();
            foreach (var item in matches)
            {
                ClientMatches.Add(item);
            }

            var selected = ClientMatches.FirstOrDefault(c => c.Id == SelectedClient?.Id)
                ?? ClientMatches.FirstOrDefault(c => c.Phone == phoneQuery)
                ?? ClientMatches.FirstOrDefault();

            SelectedClient = selected;
        }

        private async Task LoadPetsForClientAsync(int? clientId)
        {
            if (clientId is null or <= 0)
            {
                PetOptions.Clear();
                SelectedPet = null;
                return;
            }

            using var db = new VetpetContext();
            var pets = await db.Pets
                .Where(p => p.ClientId == clientId)
                .Include(p => p.PetType)
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();

            PetOptions.Clear();
            foreach (var pet in pets)
            {
                PetOptions.Add(pet);
            }

            SelectedPet = PetOptions.FirstOrDefault();
        }

        [RelayCommand]
        private void OpenAddClient()
        {
            NewClientErrorMessage = null;
            NewClientPhone = ClientPhone.Trim();
            NewClientFirstName = "";
            NewClientLastName = "";
            NewClientEmail = "";
            IsAddClientOpen = true;
        }

        [RelayCommand]
        private void CancelAddClient()
        {
            IsAddClientOpen = false;
            NewClientErrorMessage = null;
        }

        [RelayCommand]
        private async Task SaveAddClient()
        {
            NewClientErrorMessage = null;

            if (string.IsNullOrWhiteSpace(NewClientFirstName) || string.IsNullOrWhiteSpace(NewClientLastName) || string.IsNullOrWhiteSpace(NewClientPhone))
            {
                NewClientErrorMessage = "Прізвище, ім'я та телефон є обов'язковими";
                return;
            }

            using var db = new VetpetContext();
            var client = new Client
            {
                FirstName = NewClientFirstName.Trim(),
                LastName = NewClientLastName.Trim(),
                Phone = NewClientPhone.Trim(),
                Email = string.IsNullOrWhiteSpace(NewClientEmail) ? null : NewClientEmail.Trim()
            };

            db.Clients.Add(client);
            await db.SaveChangesAsync();

            IsAddClientOpen = false;
            ClientPhone = client.Phone;
            await LoadClientMatchesAsync();
            SelectedClient = ClientMatches.FirstOrDefault(c => c.Id == client.Id) ?? SelectedClient;
        }

        [RelayCommand]
        private void OpenAddPet()
        {
            if (SelectedClient == null)
            {
                ErrorMessage = "Спочатку оберіть клієнта";
                return;
            }

            ErrorMessage = null;
            NewPetErrorMessage = null;
            NewPetName = "";
            NewPetType = PetTypeOptions.FirstOrDefault();
            NewPetGender = "male";
            NewPetBirthDay = null;
            NewPetBirthMonth = null;
            NewPetBirthYear = null;
            IsAddPetOpen = true;
        }

        [RelayCommand]
        private void CancelAddPet()
        {
            IsAddPetOpen = false;
            NewPetErrorMessage = null;
        }

        [RelayCommand]
        private async Task SaveAddPet()
        {
            NewPetErrorMessage = null;

            if (SelectedClient == null)
            {
                NewPetErrorMessage = "Клієнта не знайдено";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPetName))
            {
                NewPetErrorMessage = "Кличка є обов'язковою";
                return;
            }

            if (NewPetType == null)
            {
                NewPetErrorMessage = "Оберіть вид тварини";
                return;
            }

            DateOnly? birthDate = null;
            if (NewPetBirthDay.HasValue || NewPetBirthMonth != null || NewPetBirthYear.HasValue)
            {
                if (!NewPetBirthDay.HasValue || NewPetBirthMonth == null || !NewPetBirthYear.HasValue)
                {
                    NewPetErrorMessage = "Оберіть повну дату народження або залиште порожньою";
                    return;
                }

                if (!DateOnly.TryParse($"{NewPetBirthYear:0000}-{NewPetBirthMonth.Number:00}-{NewPetBirthDay:00}", out var parsedBirthDate))
                {
                    NewPetErrorMessage = "Невірна дата народження";
                    return;
                }

                birthDate = parsedBirthDate;
            }

            using var db = new VetpetContext();
            var pet = new Pet
            {
                ClientId = SelectedClient.Id,
                Name = NewPetName.Trim(),
                PetTypeId = NewPetType.Id,
                Gender = string.IsNullOrWhiteSpace(NewPetGender) ? null : NewPetGender,
                BirthDate = birthDate
            };

            db.Pets.Add(pet);
            await db.SaveChangesAsync();

            IsAddPetOpen = false;
            await LoadPetsForClientAsync(SelectedClient.Id);
            SelectedPet = PetOptions.FirstOrDefault(p => p.Id == pet.Id) ?? SelectedPet;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            if (SelectedClient == null)
            {
                ErrorMessage = "Оберіть клієнта";
                return;
            }

            if (SelectedPet == null)
            {
                ErrorMessage = "Оберіть тварину";
                return;
            }

            if (SelectedStaff == null)
            {
                ErrorMessage = "Оберіть лікаря";
                return;
            }

            if (!SelectedDay.HasValue || SelectedMonth == null || !SelectedYear.HasValue || !SelectedHour.HasValue || !SelectedMinute.HasValue)
            {
                ErrorMessage = "Оберіть дату та час";
                return;
            }

            DateTime appointmentDate;
            try
            {
                appointmentDate = new DateTime(
                    SelectedYear.Value,
                    SelectedMonth.Number,
                    SelectedDay.Value,
                    SelectedHour.Value,
                    SelectedMinute.Value,
                    0);
            }
            catch
            {
                ErrorMessage = "Невірні дата або час";
                return;
            }

            using var db = new VetpetContext();
            db.Appointments.Add(new Appointment
            {
                ClientId = SelectedClient.Id,
                PetId = SelectedPet.Id,
                StaffId = SelectedStaff.Id,
                ServiceId = SelectedService?.Id,
                Date = appointmentDate,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Status = string.IsNullOrWhiteSpace(SelectedStatus) ? "заплановано" : SelectedStatus
            });

            await db.SaveChangesAsync();
            await _parent.HandleQuickAppointmentSavedAsync(SelectedClient.Id);
        }

        [RelayCommand]
        private void Cancel()
        {
            _parent.CloseQuickAppointment();
        }
    }

    public sealed class MonthOption
    {
        public int Number { get; }
        public string Name { get; }

        public MonthOption(int number, string name)
        {
            Number = number;
            Name = name;
        }
    }
}
