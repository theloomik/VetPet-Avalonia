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
        [ObservableProperty] private bool _createNewClient;
        [ObservableProperty] private string _clientFirstName = "";
        [ObservableProperty] private string _clientLastName = "";

        [ObservableProperty] private Pet? _selectedPet;
        [ObservableProperty] private bool _createNewPet;
        [ObservableProperty] private string _newPetName = "";
        [ObservableProperty] private PetType? _selectedPetType;
        [ObservableProperty] private string _selectedPetGender = "невідомо";

        [ObservableProperty] private Staff? _selectedStaff;
        [ObservableProperty] private Service? _selectedService;
        [ObservableProperty] private string _dateText = DateTime.Now.AddHours(1).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private string _selectedStatus = "заплановано";
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<Client> ClientMatches { get; } = new();
        public ObservableCollection<Pet> PetOptions { get; } = new();
        public ObservableCollection<PetType> PetTypeOptions { get; } = new();
        public ObservableCollection<Staff> StaffOptions { get; } = new();
        public ObservableCollection<Service> ServiceOptions { get; } = new();
        public ObservableCollection<string> PetGenderOptions { get; } = new() { "невідомо", "самець", "самка" };
        public ObservableCollection<string> StatusOptions { get; } = new() { "заплановано", "виконано", "скасовано" };

        public bool CanSelectExistingPet => !CreateNewPet && PetOptions.Count > 0;

        public QuickAppointmentViewModel(MainWindowViewModel parent)
        {
            _parent = parent;
            _ = LoadInitialOptionsAsync();
        }

        partial void OnClientPhoneChanged(string value)
        {
            _ = LoadClientMatchesAsync();
        }

        partial void OnSelectedClientChanged(Client? value)
        {
            _ = LoadPetsForClientAsync(value?.Id);
        }

        partial void OnCreateNewClientChanged(bool value)
        {
            if (value)
            {
                SelectedClient = null;
                PetOptions.Clear();
                SelectedPet = null;
                CreateNewPet = true;
                OnPropertyChanged(nameof(CanSelectExistingPet));
                return;
            }

            _ = LoadClientMatchesAsync();
        }

        partial void OnCreateNewPetChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSelectExistingPet));
        }

        private async Task LoadInitialOptionsAsync()
        {
            using var db = new VetpetContext();

            var staff = await db.Staff
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

            SelectedPetType = PetTypeOptions.FirstOrDefault();
        }

        private async Task LoadClientMatchesAsync()
        {
            if (CreateNewClient)
            {
                return;
            }

            var phoneQuery = ClientPhone.Trim();
            if (string.IsNullOrWhiteSpace(phoneQuery))
            {
                ClientMatches.Clear();
                SelectedClient = null;
                PetOptions.Clear();
                SelectedPet = null;
                OnPropertyChanged(nameof(CanSelectExistingPet));
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
            if (clientId is null or <= 0 || CreateNewClient)
            {
                PetOptions.Clear();
                SelectedPet = null;
                OnPropertyChanged(nameof(CanSelectExistingPet));
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
            CreateNewPet = PetOptions.Count == 0;

            OnPropertyChanged(nameof(CanSelectExistingPet));
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            var normalizedPhone = ClientPhone.Trim();
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                ErrorMessage = "Вкажіть номер телефону клієнта";
                return;
            }

            if (SelectedStaff == null)
            {
                ErrorMessage = "Оберіть лікаря";
                return;
            }

            if (!DateTime.TryParseExact(DateText.Trim(), "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                ErrorMessage = "Вкажіть дату у форматі ДД.ММ.РРРР ГГ:ХХ";
                return;
            }

            using var db = new VetpetContext();

            Client clientEntity;
            if (CreateNewClient)
            {
                if (string.IsNullOrWhiteSpace(ClientFirstName) || string.IsNullOrWhiteSpace(ClientLastName))
                {
                    ErrorMessage = "Для нового клієнта потрібні ім'я та прізвище";
                    return;
                }

                clientEntity = await db.Clients.FirstOrDefaultAsync(c => c.Phone == normalizedPhone) ?? new Client
                {
                    Phone = normalizedPhone,
                    FirstName = ClientFirstName.Trim(),
                    LastName = ClientLastName.Trim()
                };

                if (clientEntity.Id <= 0)
                {
                    db.Clients.Add(clientEntity);
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                if (SelectedClient == null)
                {
                    ErrorMessage = "Клієнта не знайдено. Увімкніть створення нового клієнта";
                    return;
                }

                var existingClient = await db.Clients.FirstOrDefaultAsync(c => c.Id == SelectedClient.Id);
                if (existingClient == null)
                {
                    ErrorMessage = "Клієнта не знайдено в базі";
                    return;
                }

                clientEntity = existingClient;
            }

            Pet petEntity;
            if (CreateNewPet)
            {
                if (string.IsNullOrWhiteSpace(NewPetName))
                {
                    ErrorMessage = "Вкажіть кличку тварини";
                    return;
                }

                if (SelectedPetType == null)
                {
                    ErrorMessage = "Оберіть вид тварини";
                    return;
                }

                petEntity = new Pet
                {
                    ClientId = clientEntity.Id,
                    Name = NewPetName.Trim(),
                    PetTypeId = SelectedPetType.Id,
                    Gender = SelectedPetGender == "невідомо" ? null : SelectedPetGender
                };

                db.Pets.Add(petEntity);
                await db.SaveChangesAsync();
            }
            else
            {
                if (SelectedPet == null)
                {
                    ErrorMessage = "Оберіть тварину";
                    return;
                }

                var existingPet = await db.Pets.FirstOrDefaultAsync(p => p.Id == SelectedPet.Id && p.ClientId == clientEntity.Id);
                if (existingPet == null)
                {
                    ErrorMessage = "Обрана тварина не належить цьому клієнту";
                    return;
                }

                petEntity = existingPet;
            }

            db.Appointments.Add(new Appointment
            {
                ClientId = clientEntity.Id,
                PetId = petEntity.Id,
                StaffId = SelectedStaff.Id,
                ServiceId = SelectedService?.Id,
                Date = date,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Status = string.IsNullOrWhiteSpace(SelectedStatus) ? "заплановано" : SelectedStatus
            });

            await db.SaveChangesAsync();
            await _parent.HandleQuickAppointmentSavedAsync(clientEntity.Id);
        }

        [RelayCommand]
        private void Cancel()
        {
            _parent.CloseQuickAppointment();
        }
    }
}
