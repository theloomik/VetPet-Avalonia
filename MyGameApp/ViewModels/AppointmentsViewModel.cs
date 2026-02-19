using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class AppointmentsViewModel : ViewModelBase
    {
        private List<Appointment> _allAppointments = new();
        public ObservableCollection<Appointment> Appointments { get; } = new();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    UpdateList();
            }
        }

        public AppointmentsViewModel()
        {
            _ = InitializeAsync();
        }

        private void UpdateList()
        {
            var query = _allAppointments
                .Where(a => a.Client != null &&
                            a.Client.Phone != null &&
                            a.Client.Phone.Contains(SearchText));

            Appointments.Clear();
            foreach (var item in query)
                Appointments.Add(item);
        }

        private async Task InitializeAsync()
        {
            using var db = new VetpetContext();
            _allAppointments = await db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Pet)
                .AsNoTracking()
                .ToListAsync();

            UpdateList();
        }
    }
}
