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

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddStaffViewModel? _addForm;

        public IRelayCommand AddStaffCommand { get; }

        public StaffViewModel()
        {
            AddStaffCommand = new RelayCommand(OpenAdd);
            _ = ReloadAsync();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();

        private void OpenAdd()
        {
            AddForm = new AddStaffViewModel(this);
            IsAddOpen = true;
        }

        private void UpdateList()
        {
            var q = _allStaff.Where(s =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (s.LastName != null && s.LastName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)) ||
                (s.Phone != null && s.Phone.Contains(SearchText)));
            StaffMembers.Clear();
            foreach (var item in q) StaffMembers.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allStaff = await db.Staff.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
