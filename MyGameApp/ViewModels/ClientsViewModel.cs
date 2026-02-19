using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class ClientsViewModel : ViewModelBase
    {
        private List<Client> _allClients = new();
        public ObservableCollection<Client> Clients { get; } = new();

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

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "А→Я" : "Я→А";

        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand AddClientCommand { get; }

        public ClientsViewModel()
        {
            ToggleSortCommand = new RelayCommand(ToggleSort);
            AddClientCommand = new RelayCommand(OpenAddClient);
            _ = InitializeAsync();
        }

        private void ToggleSort()
        {
            _sortAsc = !_sortAsc;
            OnPropertyChanged(nameof(SortLabel));
            UpdateList();
        }

        private void OpenAddClient() { /* заглушка */ }

        private void UpdateList()
        {
            var query = _allClients
                .Where(c => c.LastName != null &&
                            c.LastName.ToLower().Contains(SearchText.ToLower()));
            query = _sortAsc
                ? query.OrderBy(c => c.LastName)
                : query.OrderByDescending(c => c.LastName);

            Clients.Clear();
            foreach (var item in query)
                Clients.Add(item);
        }

        private async Task InitializeAsync()
        {
            using var db = new VetpetContext();
            _allClients = await db.Clients.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}