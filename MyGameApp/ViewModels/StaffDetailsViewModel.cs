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
    public partial class StaffDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? _mainVm;

        [ObservableProperty] private Staff _selectedStaff = null!;
        [ObservableProperty] private string? _loadError;
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private string _editFirstName = "";
        [ObservableProperty] private string _editLastName = "";
        [ObservableProperty] private string _editPhone = "";
        [ObservableProperty] private string _editWorkDays = "";
        [ObservableProperty] private string _editStartTime = "09:00";
        [ObservableProperty] private string _editEndTime = "18:00";
        [ObservableProperty] private StaffPosition? _selectedPosition;
        [ObservableProperty] private string? _editError;

        public ObservableCollection<StaffPosition> PositionOptions { get; } = new();

        public string FullName => $"{SelectedStaff.LastName} {SelectedStaff.FirstName}".Trim();
        public string Phone => string.IsNullOrWhiteSpace(SelectedStaff.Phone) ? "—" : SelectedStaff.Phone;
        public string Position => SelectedStaff.StaffPosition?.Position ?? "—";
        public string Salary => SelectedStaff.StaffPosition != null
            ? $"{SelectedStaff.StaffPosition.Salary:0.##} ₴"
            : "—";
        public string WorkDays => FormatWorkDays(SelectedStaff.WorkDays);
        public string WorkHours => $"{FormatTime(SelectedStaff.StartTime)} - {FormatTime(SelectedStaff.EndTime)}";
        public string CreatedAt => SelectedStaff.CreatedAt?.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "—";
        public bool IsArchived => StaffArchive.IsArchived(SelectedStaff);

        public StaffDetailsViewModel(Staff? staff = null, MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            SelectedStaff = staff ?? new Staff();

            if (SelectedStaff.Id > 0)
            {
                _ = LoadAsync();
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            if (_mainVm == null)
                return;

            _mainVm.CurrentViewModel = new StaffViewModel(_mainVm);
        }

        [RelayCommand]
        private void StartEdit()
        {
            EditFirstName = SelectedStaff.FirstName ?? "";
            EditLastName = SelectedStaff.LastName ?? "";
            EditPhone = SelectedStaff.Phone ?? "";
            EditWorkDays = SelectedStaff.WorkDays == StaffArchive.ArchivedMarker ? "" : (SelectedStaff.WorkDays ?? "");
            EditStartTime = FormatTimeForInput(SelectedStaff.StartTime);
            EditEndTime = FormatTimeForInput(SelectedStaff.EndTime);
            SelectedPosition = PositionOptions.FirstOrDefault(p => p.Id == SelectedStaff.StaffPositionId);
            EditError = null;
            IsEditMode = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditMode = false;
            EditError = null;
        }

        [RelayCommand]
        private async Task SaveEdit()
        {
            EditError = null;

            if (string.IsNullOrWhiteSpace(EditFirstName) ||
                string.IsNullOrWhiteSpace(EditLastName) ||
                string.IsNullOrWhiteSpace(EditPhone) ||
                SelectedPosition == null)
            {
                EditError = "Last name, first name, phone and position are required";
                return;
            }

            if (!TimeOnly.TryParse(EditStartTime, CultureInfo.InvariantCulture, out var startTime))
            {
                EditError = "Start time must be in HH:mm format";
                return;
            }

            if (!TimeOnly.TryParse(EditEndTime, CultureInfo.InvariantCulture, out var endTime))
            {
                EditError = "End time must be in HH:mm format";
                return;
            }

            using var db = new VetpetContext();
            var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == SelectedStaff.Id);
            if (staff == null)
            {
                EditError = "Staff member not found";
                return;
            }

            staff.FirstName = EditFirstName.Trim();
            staff.LastName = EditLastName.Trim();
            staff.Phone = EditPhone.Trim();
            staff.StaffPositionId = SelectedPosition.Id;
            staff.WorkDays = string.IsNullOrWhiteSpace(EditWorkDays) ? null : EditWorkDays.Trim();
            staff.StartTime = startTime;
            staff.EndTime = endTime;

            await db.SaveChangesAsync();

            IsEditMode = false;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ArchiveStaff()
        {
            using var db = new VetpetContext();
            var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == SelectedStaff.Id);
            if (staff == null)
            {
                LoadError = "Staff member not found";
                return;
            }

            staff.WorkDays = StaffArchive.ArchivedMarker;
            await db.SaveChangesAsync();

            GoBack();
        }

        private async Task LoadAsync()
        {
            try
            {
                using var db = new VetpetContext();

                var positions = await db.StaffPositions
                    .OrderBy(p => p.Position)
                    .AsNoTracking()
                    .ToListAsync();

                PositionOptions.Clear();
                foreach (var position in positions)
                    PositionOptions.Add(position);

                var staff = await db.Staff
                    .Include(s => s.StaffPosition)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == SelectedStaff.Id);

                if (staff == null)
                {
                    LoadError = "Працівника не знайдено";
                    return;
                }

                SelectedStaff = staff;
                LoadError = null;
                RefreshDerivedState();
            }
            catch (Exception ex)
            {
                LoadError = $"Не вдалося завантажити профіль працівника: {ex.Message}";
            }
        }

        partial void OnSelectedStaffChanged(Staff value) => RefreshDerivedState();

        private void RefreshDerivedState()
        {
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(Salary));
            OnPropertyChanged(nameof(WorkDays));
            OnPropertyChanged(nameof(WorkHours));
            OnPropertyChanged(nameof(CreatedAt));
            OnPropertyChanged(nameof(IsArchived));
        }

        private static string FormatTime(TimeOnly? time) =>
            time?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "—";

        private static string FormatTimeForInput(TimeOnly? time) =>
            time?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "09:00";

        private static string FormatWorkDays(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == StaffArchive.ArchivedMarker)
                return "—";

            var parts = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(MapDayToken);

            return string.Join(", ", parts);
        }

        private static string MapDayToken(string token) => token switch
        {
            "Mon" or "Пн" or "РџРЅ" => "Понеділок",
            "Tue" or "Вт" or "Р’С‚" => "Вівторок",
            "Wed" or "Ср" or "РЎСЂ" => "Середа",
            "Thu" or "Чт" or "Р§С‚" => "Четвер",
            "Fri" or "Пт" or "РџС‚" => "Пʼятниця",
            "Sat" or "Сб" or "РЎР±" => "Субота",
            "Sun" or "Нд" or "РќРґ" => "Неділя",
            _ => token
        };
    }
}
