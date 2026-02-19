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
    public partial class AddStaffViewModel : ViewModelBase
    {
        private readonly StaffViewModel _parent;

        // Основні дані
        [ObservableProperty] private string _firstName = "";
        [ObservableProperty] private string _lastName = "";
        [ObservableProperty] private string _phone = "";
        [ObservableProperty] private StaffPosition? _selectedPosition;
        [ObservableProperty] private string? _errorMessage;

        // Робочі дні
        [ObservableProperty] private bool _dayMon = true;
        [ObservableProperty] private bool _dayTue = true;
        [ObservableProperty] private bool _dayWed = true;
        [ObservableProperty] private bool _dayThu = true;
        [ObservableProperty] private bool _dayFri = true;
        [ObservableProperty] private bool _daySat = false;
        [ObservableProperty] private bool _daySun = false;

        // Робочі години (UI → string)
        [ObservableProperty] private string _startTime = "09:00";
        [ObservableProperty] private string _endTime = "18:00";

        // Додавання нової посади
        [ObservableProperty] private bool _isAddingPosition;
        [ObservableProperty] private string _newPositionName = "";
        [ObservableProperty] private string _newPositionSalary = "";
        [ObservableProperty] private string? _positionError;

        public ObservableCollection<StaffPosition> Positions { get; } = new();

        public AddStaffViewModel(StaffViewModel parent)
        {
            _parent = parent;
            _ = LoadPositionsAsync();
        }

        // ---------- Завантаження посад ----------
        private async Task LoadPositionsAsync()
        {
            using var db = new VetpetContext();
            var list = await db.StaffPositions.AsNoTracking().ToListAsync();

            Positions.Clear();
            foreach (var p in list)
                Positions.Add(p);
        }

        // ---------- Робочі дні ----------
        private string BuildWorkDays()
        {
            var days = new[]
            {
                (DayMon, "Пн"),
                (DayTue, "Вт"),
                (DayWed, "Ср"),
                (DayThu, "Чт"),
                (DayFri, "Пт"),
                (DaySat, "Сб"),
                (DaySun, "Нд")
            };

            var selected = days
                .Where(d => d.Item1)
                .Select(d => d.Item2)
                .ToList();

            return selected.Any()
                ? string.Join(", ", selected)
                : "—";
        }

        // ---------- Посади ----------
        [RelayCommand]
        private void ShowAddPosition()
        {
            NewPositionName = "";
            NewPositionSalary = "";
            PositionError = null;
            IsAddingPosition = true;
        }

        [RelayCommand]
        private void CancelAddPosition()
        {
            IsAddingPosition = false;
        }

        [RelayCommand]
        private async Task SavePosition()
        {
            if (string.IsNullOrWhiteSpace(NewPositionName))
            {
                PositionError = "Введіть назву посади";
                return;
            }

            if (!decimal.TryParse(
                    NewPositionSalary.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var salary))
            {
                PositionError = "Невірний формат зарплати";
                return;
            }

            using var db = new VetpetContext();
            var pos = new StaffPosition
            {
                Position = NewPositionName.Trim(),
                Salary = salary
            };

            db.StaffPositions.Add(pos);
            await db.SaveChangesAsync();

            Positions.Add(pos);
            SelectedPosition = pos;
            IsAddingPosition = false;
        }

        // ---------- Збереження працівника ----------
        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Phone) ||
                SelectedPosition == null)
            {
                ErrorMessage = "Прізвище, ім'я, телефон та посада є обов'язковими";
                return;
            }

            var start = TimeOnly.TryParse(StartTime, out var st)
                ? st
                : new TimeOnly(9, 0);

            var end = TimeOnly.TryParse(EndTime, out var et)
                ? et
                : new TimeOnly(18, 0);

            using var db = new VetpetContext();
            var staff = new Staff
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Phone = Phone.Trim(),
                StaffPositionId = SelectedPosition.Id,
                WorkDays = BuildWorkDays(),
                StartTime = start,
                EndTime = end
            };

            db.Staff.Add(staff);
            await db.SaveChangesAsync();

            await _parent.ReloadAsync();
            _parent.IsAddOpen = false;
        }

        [RelayCommand]
        private void Cancel()
        {
            _parent.IsAddOpen = false;
        }
    }
}
