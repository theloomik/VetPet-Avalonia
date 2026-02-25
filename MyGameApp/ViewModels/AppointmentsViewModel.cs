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
        public ObservableCollection<AppointmentListRow> Appointments { get; } = new();

        [ObservableProperty] private string _searchText = "";

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "Дата ↓" : "Дата ↑";

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
            var q = _allAppointments.Where(a =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (!string.IsNullOrWhiteSpace(a.ClientPhone) && a.ClientPhone.Contains(SearchText)));

            q = _sortAsc 
                ? q.OrderBy(a => a.Source.Date) 
                : q.OrderByDescending(a => a.Source.Date);

            Appointments.Clear();
            foreach (var item in q) Appointments.Add(item);
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
                return;

            _mainVm.OpenClientDetails(client);
        }
    }

    public class AppointmentListRow
    {
        public Appointment Source { get; }
        public string ClientPhone => Source.Client?.Phone ?? "—";
        public string PetName => Source.Pet?.Name ?? "—";
        public string Status => Source.Status ?? "—";
        public string StatusColor => Source.Status switch
        {
            "виконано" => "#4A7C59",
            "скасовано" => "#7C4A4A",
            _ => "#7C6E4A"
        };

        public AppointmentListRow(Appointment appointment)
        {
            Source = appointment;
        }
    }
}
