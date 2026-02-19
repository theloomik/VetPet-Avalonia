using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class StockViewModel : ViewModelBase
    {
        private List<Stock> _allStock = new();
        public ObservableCollection<Stock> Stocks { get; } = new();

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

        public StockViewModel()
        {
            _ = InitializeAsync();
        }

        private void UpdateList()
        {
            var query = _allStock
                .Where(s => s.Medicine != null &&
                            s.Medicine.Name != null &&
                            s.Medicine.Name.ToLower().Contains(SearchText.ToLower()));

            Stocks.Clear();
            foreach (var item in query)
                Stocks.Add(item);
        }

        private async Task InitializeAsync()
        {
            using var db = new VetpetContext();
            _allStock = await db.Stocks
                .Include(s => s.Medicine)
                .AsNoTracking()
                .ToListAsync();

            UpdateList();
        }
    }
}
