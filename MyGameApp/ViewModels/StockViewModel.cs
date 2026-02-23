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
    public partial class StockViewModel : ViewModelBase
    {
        private List<Stock> _allStock = new();
        public ObservableCollection<Stock> Stocks { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddStockViewModel? _addForm;

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "А → Я" : "Я → А";

        public IRelayCommand ToggleSortCommand { get; }

        public IRelayCommand AddStockCommand { get; }

        public StockViewModel()
        {
            ToggleSortCommand = new RelayCommand(ToggleSort);
            AddStockCommand = new RelayCommand(OpenAdd);
            _ = ReloadAsync();
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
            AddForm = new AddStockViewModel(this);
            IsAddOpen = true;
        }

        private void UpdateList()
        {
            var q = _allStock.Where(s =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (s.Medicine?.Name != null && s.Medicine.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)));
            q = _sortAsc ? q.OrderBy(c => c.Medicine?.Name) : q.OrderByDescending(c => c.Medicine?.Name);
            Stocks.Clear();
            foreach (var item in q) Stocks.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();
            _allStock = await db.Stocks.Include(s => s.Medicine).AsNoTracking().ToListAsync();
            UpdateList();
        }
    }
}
