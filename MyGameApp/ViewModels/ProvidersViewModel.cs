using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class ProvidersViewModel : ViewModelBase
    {
        private List<Provider> _allProviders = new();
        public ObservableCollection<Provider> Providers { get; } = new();

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

        public ProvidersViewModel()
        {
            _ = InitializeAsync();
        }

        private void UpdateList()
        {
            var query = _allProviders
                .Where(p => p.Name != null &&
                            p.Name.ToLower().Contains(SearchText.ToLower()));
            Providers.Clear();
            foreach (var item in query)
                Providers.Add(item);
        }

        private async Task InitializeAsync()
        {
            using var db = new VetpetContext();
            _allProviders = await db.Providers
                .AsNoTracking()
                .ToListAsync();
            UpdateList();
        }
    }
}