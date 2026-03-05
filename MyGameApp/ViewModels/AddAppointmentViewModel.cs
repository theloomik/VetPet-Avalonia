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
    public partial class AddAppointmentViewModel : ViewModelBase
    {
        private readonly ClientDetailsViewModel _parent;

        [ObservableProperty] private Pet? _selectedPet;
        [ObservableProperty] private Staff? _selectedStaff;
        [ObservableProperty] private Service? _selectedService;
        [ObservableProperty] private int? _selectedDay;
        [ObservableProperty] private int? _selectedMonth;
        [ObservableProperty] private int? _selectedYear;
        [ObservableProperty] private int? _selectedHour;
        [ObservableProperty] private int? _selectedMinute;
        [ObservableProperty] private string _description = "";
        [ObservableProperty] private string _selectedStatus = "заплановано";
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<Pet> PetOptions { get; } = new();
        public ObservableCollection<Staff> StaffOptions { get; } = new();
        public ObservableCollection<Service> ServiceOptions { get; } = new();
        public ObservableCollection<int> DayOptions { get; } = new();
        public ObservableCollection<int> MonthOptions { get; } = new();
        public ObservableCollection<int> YearOptions { get; } = new();
        public ObservableCollection<int> HourOptions { get; } = new();
        public ObservableCollection<int> MinuteOptions { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new() { "заплановано", "виконано", "скасовано" };

        public AddAppointmentViewModel(ClientDetailsViewModel parent)
        {
            _parent = parent;
            SeedDateOptions();
            SetInitialDateTime();
            _ = LoadOptionsAsync();
        }

        private void SeedDateOptions()
        {
            DayOptions.Clear();
            for (var day = 1; day <= 31; day++)
            {
                DayOptions.Add(day);
            }

            MonthOptions.Clear();
            for (var month = 1; month <= 12; month++)
            {
                MonthOptions.Add(month);
            }

            YearOptions.Clear();
            for (var year = DateTime.Now.Year + 2; year >= DateTime.Now.Year - 2; year--)
            {
                YearOptions.Add(year);
            }

            HourOptions.Clear();
            for (var hour = 0; hour <= 23; hour++)
            {
                HourOptions.Add(hour);
            }

            MinuteOptions.Clear();
            for (var minute = 0; minute <= 59; minute += 5)
            {
                MinuteOptions.Add(minute);
            }
        }

        private void SetInitialDateTime()
        {
            var now = DateTime.Now.AddHours(1);
            var roundedMinute = (now.Minute / 5) * 5;

            SelectedDay = now.Day;
            SelectedMonth = now.Month;
            SelectedYear = now.Year;
            SelectedHour = now.Hour;
            SelectedMinute = roundedMinute;
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
                .Where(s => s.WorkDays != StaffArchive.ArchivedMarker)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .AsNoTracking()
                .ToListAsync();

            var services = await db.Services
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

            PetOptions.Clear();
            foreach (var pet in pets)
            {
                PetOptions.Add(pet);
            }

            StaffOptions.Clear();
            foreach (var staffMember in staff)
            {
                StaffOptions.Add(staffMember);
            }

            ServiceOptions.Clear();
            foreach (var service in services)
            {
                ServiceOptions.Add(service);
            }

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

            if (!SelectedDay.HasValue || !SelectedMonth.HasValue || !SelectedYear.HasValue || !SelectedHour.HasValue || !SelectedMinute.HasValue)
            {
                ErrorMessage = "Вкажіть дату та час";
                return;
            }

            DateTime date;
            try
            {
                date = new DateTime(
                    SelectedYear.Value,
                    SelectedMonth.Value,
                    SelectedDay.Value,
                    SelectedHour.Value,
                    SelectedMinute.Value,
                    0);
            }
            catch (ArgumentOutOfRangeException)
            {
                ErrorMessage = "Некоректна дата або час";
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
