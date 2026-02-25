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
    public partial class ProvidersViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? _mainVm;
        private List<Provider> _allProviders = new();
        public ObservableCollection<Provider> Providers { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddProviderViewModel? _addForm;

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "А → Я" : "Я → А";

        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand AddProviderCommand { get; }
        public IRelayCommand<Provider?> GoToDetailsCommand { get; }

        public ProvidersViewModel(MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            ToggleSortCommand = new RelayCommand(ToggleSort);
            AddProviderCommand = new RelayCommand(OpenAdd);
            GoToDetailsCommand = new RelayCommand<Provider?>(GoToDetails);
            _ = ReloadAsync();
        }

        private void ToggleSort()
        {
            _sortAsc = !_sortAsc;
            OnPropertyChanged(nameof(SortLabel));
            UpdateList();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();

        private void OpenAdd()
        {
            AddForm = new AddProviderViewModel(this);
            IsAddOpen = true;
        }

        public void GoToDetails(Provider? provider)
        {
            if (_mainVm == null || provider == null)
                return;

            _mainVm.OpenProviderDetails(provider);
        }

        private void UpdateList()
        {
            var q = _allProviders.Where(p =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (p.Name != null && p.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)));
            q = _sortAsc ? q.OrderBy(c => c.Name) : q.OrderByDescending(c => c.Name);
            Providers.Clear();
            foreach (var item in q)
                Providers.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allProviders = await db.Providers.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
