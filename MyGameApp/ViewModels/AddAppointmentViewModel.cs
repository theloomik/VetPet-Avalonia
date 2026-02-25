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
    public partial class AddAppointmentViewModel : ViewModelBase
    {
        private readonly ClientDetailsViewModel _parent;

        [ObservableProperty] private Pet? _selectedPet;
        [ObservableProperty] private Staff? _selectedStaff;
        [ObservableProperty] private Service? _selectedService;
        [ObservableProperty] private string _dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private string _selectedStatus = "заплановано";
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<Pet> PetOptions { get; } = new();
        public ObservableCollection<Staff> StaffOptions { get; } = new();
        public ObservableCollection<Service> ServiceOptions { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new() { "заплановано", "виконано", "скасовано" };

        public AddAppointmentViewModel(ClientDetailsViewModel parent)
        {
            _parent = parent;
            _ = LoadOptionsAsync();
        }

        private async Task LoadOptionsAsync()
        {
            using var db = new VetpetContext();

            var pets = await db.Pets
                .Where(p => p.ClientId == _parent.SelectedClient.Id)
                .Include(p => p.PetType)
                .OrderBy(p => p.Name)
                .AsNoTracking()
                .ToListAsync();

            var staff = await db.Staff
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .AsNoTracking()
                .ToListAsync();

            var services = await db.Services
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

            PetOptions.Clear();
            foreach (var p in pets) PetOptions.Add(p);

            StaffOptions.Clear();
            foreach (var s in staff) StaffOptions.Add(s);

            ServiceOptions.Clear();
            foreach (var s in services) ServiceOptions.Add(s);

            SelectedPet = PetOptions.FirstOrDefault();
            SelectedStaff = StaffOptions.FirstOrDefault();
            SelectedService = ServiceOptions.FirstOrDefault();
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            if (SelectedPet == null)
            {
                ErrorMessage = "Оберіть тварину";
                return;
            }

            if (SelectedStaff == null)
            {
                ErrorMessage = "Оберіть працівника";
                return;
            }

            if (!DateTime.TryParseExact(DateText.Trim(), "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                ErrorMessage = "Вкажіть дату у форматі DD.MM.YYYY HH:MM";
                return;
            }

            using var db = new VetpetContext();
            db.Appointments.Add(new Appointment
            {
                ClientId = _parent.SelectedClient.Id,
                PetId = SelectedPet.Id,
                StaffId = SelectedStaff.Id,
                ServiceId = SelectedService?.Id,
                Date = date,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Status = string.IsNullOrWhiteSpace(SelectedStatus) ? "заплановано" : SelectedStatus
            });

            await db.SaveChangesAsync();
            await _parent.LoadAppointmentsAsync();
            _parent.IsAddAppointmentOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddAppointmentOpen = false;
    }
}
