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
    public partial class StaffViewModel : ViewModelBase
    {
        private List<Staff> _allStaff = new();
        public ObservableCollection<Staff> StaffMembers { get; } = new();

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

        public IRelayCommand AddStaffCommand { get; }

        public StaffViewModel()
        {
            AddStaffCommand = new RelayCommand(OpenAddStaff);
            _ = InitializeAsync();
        }

        private void UpdateList()
        {
            var query = _allStaff
                .Where(s => s.Phone != null && s.Phone.Contains(SearchText));

            StaffMembers.Clear();
            foreach (var item in query)
                StaffMembers.Add(item);
        }

        private void OpenAddStaff()
        {
            // заглушка
        }

        private async Task InitializeAsync()
        {
            using var db = new VetpetContext();
            _allStaff = await db.Staff.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
