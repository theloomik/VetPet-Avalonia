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
    public partial class ClientsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainVm;

        private List<Client> _allClients = new();
        public ObservableCollection<Client> Clients { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddClientViewModel? _addForm;
 
        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "А → Я" : "Я → А";

        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand AddClientCommand { get; }


        public ClientsViewModel(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm;
            ToggleSortCommand = new RelayCommand(ToggleSort);
            AddClientCommand = new RelayCommand(OpenAdd);
            _ = ReloadAsync();
        }

        [RelayCommand]
        public void GoToDetails(Client? client)
        {
            if (client == null)
                return;

            _mainVm.OpenClientDetails(client);
        }
 
        partial void OnSearchTextChanged(string value) => UpdateList();

        private void ToggleSort()
        {
            _sortAsc = !_sortAsc;
            OnPropertyChanged(nameof(SortLabel));
            UpdateList();
        }

        private void OpenAdd()
        {
            AddForm = new AddClientViewModel(this);
            IsAddOpen = true;
        }

        private void UpdateList()
        {
            var q = _allClients.Where(c =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (c.LastName != null && c.LastName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)) ||
                (c.Phone != null && c.Phone.Contains(SearchText)));
            q = _sortAsc ? q.OrderBy(c => c.LastName) : q.OrderByDescending(c => c.LastName);
            Clients.Clear();
            foreach (var item in q) Clients.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allClients = await db.Clients.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
