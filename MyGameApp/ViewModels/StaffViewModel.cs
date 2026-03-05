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
        private readonly MainWindowViewModel? _mainVm;
        private List<Staff> _allStaff = new();
        public ObservableCollection<Staff> StaffMembers { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddStaffViewModel? _addForm;


        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "А → Я" : "Я → А";

        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand AddStaffCommand { get; }
        public IRelayCommand<Staff?> GoToDetailsCommand { get; }

        public StaffViewModel(MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            ToggleSortCommand = new RelayCommand(ToggleSort);
            AddStaffCommand = new RelayCommand(OpenAdd);
            GoToDetailsCommand = new RelayCommand<Staff?>(GoToDetails);
            _ = ReloadAsync();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();

        public void GoToDetails(Staff? staff)
        {
            if (staff == null || _mainVm == null)
                return;

            _mainVm.OpenStaffDetails(staff);
        }

        private void OpenAdd()
        {
            AddForm = new AddStaffViewModel(this);
            IsAddOpen = true;
        }

        private void ToggleSort()
        {
            _sortAsc = !_sortAsc;
            OnPropertyChanged(nameof(SortLabel));
            UpdateList();
        }

        private void UpdateList()
        {
            var q = _allStaff.Where(s =>
                !StaffArchive.IsArchived(s) &&
                (string.IsNullOrWhiteSpace(SearchText) ||
                 (s.FirstName != null && s.FirstName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)) ||
                 (s.LastName != null && s.LastName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)) ||
                 (s.Phone != null && s.Phone.Contains(SearchText))));
            q = _sortAsc ? q.OrderBy(c => c.LastName) : q.OrderByDescending(c => c.LastName);
            StaffMembers.Clear();
            foreach (var item in q)
                StaffMembers.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allStaff = await db.Staff.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
