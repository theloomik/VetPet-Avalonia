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
        private List<Appointment> _allAppointments = new();
        public ObservableCollection<Appointment> Appointments { get; } = new();

        [ObservableProperty] private string _searchText = "";

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "Дата ↓" : "Дата ↑";

        public IRelayCommand ToggleSortCommand { get; }

        public AppointmentsViewModel()
        {
            ToggleSortCommand = new RelayCommand(ToggleSort);
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
                (a.Client?.Phone != null && a.Client.Phone.Contains(SearchText)));

            q = _sortAsc 
                ? q.OrderBy(a => a.Date) 
                : q.OrderByDescending(a => a.Date);

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
                .ToListAsync();
            
            UpdateList();
        }
    }
}