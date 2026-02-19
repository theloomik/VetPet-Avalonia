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
        private List<Provider> _allProviders = new();
        public ObservableCollection<Provider> Providers { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddProviderViewModel? _addForm;

        public IRelayCommand AddProviderCommand { get; }

        public ProvidersViewModel()
        {
            AddProviderCommand = new RelayCommand(OpenAdd);
            _ = ReloadAsync();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();

        private void OpenAdd()
        {
            AddForm = new AddProviderViewModel(this);
            IsAddOpen = true;
        }

        private void UpdateList()
        {
            var q = _allProviders.Where(p =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (p.Name != null && p.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)));
            Providers.Clear();
            foreach (var item in q) Providers.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allProviders = await db.Providers.AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
