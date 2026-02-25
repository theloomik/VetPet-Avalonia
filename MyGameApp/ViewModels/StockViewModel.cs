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
        private readonly MainWindowViewModel? _mainVm;
        private List<StockRow> _allStock = new();
        public ObservableCollection<StockRow> Stocks { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen = false;
        [ObservableProperty] private AddStockViewModel? _addForm;
        [ObservableProperty] private bool _isEditOpen = false;
        [ObservableProperty] private EditStockItemViewModel? _editForm;

        private bool _sortAsc = true;
        public string SortLabel => _sortAsc ? "А → Я" : "Я → А";
        public bool IsAnyModalOpen => IsAddOpen || IsEditOpen;

        public IRelayCommand ToggleSortCommand { get; }
        public IRelayCommand AddStockCommand { get; }
        public IRelayCommand<StockRow?> GoToProviderCommand { get; }
        public IRelayCommand<StockRow?> OpenEditCommand { get; }

        public StockViewModel(MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            ToggleSortCommand = new RelayCommand(ToggleSort);
            AddStockCommand = new RelayCommand(OpenAdd);
            GoToProviderCommand = new RelayCommand<StockRow?>(GoToProvider);
            OpenEditCommand = new RelayCommand<StockRow?>(OpenEdit);
            _ = ReloadAsync();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();
        partial void OnIsAddOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsEditOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));

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

        private void OpenEdit(StockRow? row)
        {
            if (row == null)
                return;

            EditForm = new EditStockItemViewModel(this, row);
            IsEditOpen = true;
        }

        private void GoToProvider(StockRow? row)
        {
            if (_mainVm == null || row?.Provider == null)
                return;

            _mainVm.OpenProviderDetails(row.Provider);
        }

        private void UpdateList()
        {
            var q = _allStock.Where(s =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (s.MedicineName != null && s.MedicineName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)));
            q = _sortAsc ? q.OrderBy(c => c.MedicineName) : q.OrderByDescending(c => c.MedicineName);
            Stocks.Clear();
            foreach (var item in q)
                Stocks.Add(item);
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();

            var stocks = await db.Stocks
                .Include(s => s.Medicine)
                .AsNoTracking()
                .ToListAsync();

            var medicineIds = stocks.Select(s => s.MedicineId).Distinct().ToList();
            var orderItems = await db.ProviderOrderItems
                .Where(i => medicineIds.Contains(i.MedicineId))
                .Include(i => i.Order)
                .ThenInclude(o => o.Provider)
                .AsNoTracking()
                .ToListAsync();

            var providerByMedicine = orderItems
                .GroupBy(i => i.MedicineId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Order.Date).Select(x => x.Order.Provider).FirstOrDefault());

            _allStock = stocks
                .Select(s => new StockRow(
                    s,
                    providerByMedicine.TryGetValue(s.MedicineId, out var provider) ? provider : null))
                .ToList();

            UpdateList();
        }
    }

    public class StockRow
    {
        public Stock Source { get; }
        public Provider? Provider { get; }
        public string MedicineName => Source.Medicine?.Name ?? "—";
        public string Price => $"{Source.Medicine?.Price ?? 0:0.00} ₴";
        public string Quantity => $"x{Source.Quantity}";
        public string ProviderName => Provider?.Name ?? "Провайдер не вказаний";
        public bool HasProvider => Provider != null;

        public StockRow(Stock source, Provider? provider)
        {
            Source = source;
            Provider = provider;
        }
    }
}
