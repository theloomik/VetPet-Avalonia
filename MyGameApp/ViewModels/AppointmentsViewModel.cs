using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class AppointmentsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? _mainVm;
        private List<AppointmentListRow> _allAppointments = new();

        [ObservableProperty] private string _searchText = "";

        public ObservableCollection<AppointmentGroupRow> AppointmentGroups { get; } = new();
        public bool HasAppointments => AppointmentGroups.Count > 0;

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "Дата ↑" : "Дата ↓";

        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand<AppointmentListRow?> GoToClientDetailsCommand { get; }

        public AppointmentsViewModel(MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            ToggleSortCommand = new RelayCommand(ToggleSort);
            GoToClientDetailsCommand = new RelayCommand<AppointmentListRow?>(GoToClientDetails);
            _ = ReloadAsync();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();

        private void ToggleSort()
        {
            _sortAsc = !_sortAsc;
            OnPropertyChanged(nameof(SortLabel));
            UpdateList();
        }

        private void UpdateList()
        {
            var query = SearchText.Trim();

            var filtered = _allAppointments
                .Where(a => string.IsNullOrWhiteSpace(query)
                    || ContainsInsensitive(a.ClientPhone, query)
                    || ContainsInsensitive(a.ClientFullName, query)
                    || ContainsInsensitive(a.PetName, query))
                .ToList();

            filtered = _sortAsc
                ? filtered.OrderBy(a => a.Source.Date).ToList()
                : filtered.OrderByDescending(a => a.Source.Date).ToList();

            var grouped = filtered
                .GroupBy(GetGroupKey)
                .ToDictionary(g => g.Key, g => g.ToList());

            AppointmentGroups.Clear();
            AddGroup(grouped, AppointmentGroupKey.Today, "Сьогодні", "#4A7C59");
            AddGroup(grouped, AppointmentGroupKey.Tomorrow, "Завтра", "#7C6E4A");
            AddGroup(grouped, AppointmentGroupKey.Future, "Майбутні", "#4A6A7C");
            AddGroup(grouped, AppointmentGroupKey.Past, "Минулі", "#6A6A6A");
            AddGroup(grouped, AppointmentGroupKey.Cancelled, "Скасовані", "#7C4A4A");

            OnPropertyChanged(nameof(HasAppointments));
        }

        private static bool ContainsInsensitive(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private void AddGroup(
            IReadOnlyDictionary<AppointmentGroupKey, List<AppointmentListRow>> grouped,
            AppointmentGroupKey key,
            string title,
            string markerColor)
        {
            if (!grouped.TryGetValue(key, out var items) || items.Count == 0)
            {
                return;
            }

            AppointmentGroups.Add(new AppointmentGroupRow(title, markerColor, items));
        }

        private static AppointmentGroupKey GetGroupKey(AppointmentListRow row)
        {
            if (row.IsCancelled)
            {
                return AppointmentGroupKey.Cancelled;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var rowDate = DateOnly.FromDateTime(row.Source.Date);

            if (rowDate < today)
            {
                return AppointmentGroupKey.Past;
            }

            if (rowDate == today)
            {
                return AppointmentGroupKey.Today;
            }

            if (rowDate == today.AddDays(1))
            {
                return AppointmentGroupKey.Tomorrow;
            }

            return AppointmentGroupKey.Future;
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allAppointments = await db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Staff)
                .Include(a => a.Pet)
                .AsNoTracking()
                .Select(a => new AppointmentListRow(a))
                .ToListAsync();

            UpdateList();
        }

        private void GoToClientDetails(AppointmentListRow? row)
        {
            var client = row?.Source.Client;
            if (_mainVm == null || client == null || client.Id <= 0)
            {
                return;
            }

            _mainVm.OpenClientDetails(client);
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

    public class AppointmentGroupRow
    {
        public string Title { get; }
        public string MarkerColor { get; }
        public ObservableCollection<AppointmentListRow> Items { get; } = new();

        public AppointmentGroupRow(string title, string markerColor, IEnumerable<AppointmentListRow> items)
        {
            Title = title;
            MarkerColor = markerColor;
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
    }

    public class AppointmentListRow
    {
        public Appointment Source { get; }
        public string ClientPhone => Source.Client?.Phone ?? "—";
        public string ClientFullName => $"{Source.Client?.LastName} {Source.Client?.FirstName}".Trim();
        public string PetName => Source.Pet?.Name ?? "—";
        public string StaffName => Source.Staff != null ? $"{Source.Staff.LastName} {Source.Staff.FirstName}".Trim() : "—";
        public string Status => Source.Status ?? "—";

        public bool IsCancelled => string.Equals(Source.Status?.Trim(), "скасовано", StringComparison.OrdinalIgnoreCase);

        public string StatusColor => Source.Status?.Trim().ToLowerInvariant() switch
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

                var today = DateOnly.FromDateTime(DateTime.Today);
                var date = DateOnly.FromDateTime(Source.Date);

                if (date < today)
                {
                    return "#6A6A6A";
                }

                if (date == today)
                {
                    return "#4A7C59";
                }

                if (date == today.AddDays(1))
                {
                    return "#7C6E4A";
                }

                return "#4A6A7C";
            }
        }

        public AppointmentListRow(Appointment appointment)
        {
            Source = appointment;
        }
    }
}
