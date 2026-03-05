using System;
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
        private const int LowStockThreshold = 10;

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
            {
                return;
            }

            EditForm = new EditStockItemViewModel(this, row);
            IsEditOpen = true;
        }

        private void GoToProvider(StockRow? row)
        {
            if (_mainVm == null || row?.Provider == null)
            {
                return;
            }

            _mainVm.OpenProviderDetails(row.Provider);
        }

        private void UpdateList()
        {
            var q = _allStock.Where(s =>
                string.IsNullOrWhiteSpace(SearchText) ||
                (s.MedicineName != null && s.MedicineName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (s.ProviderName != null && s.ProviderName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            q = _sortAsc ? q.OrderBy(c => c.MedicineName) : q.OrderByDescending(c => c.MedicineName);

            Stocks.Clear();
            foreach (var item in q)
            {
                Stocks.Add(item);
            }
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

            var latestSupplyByMedicine = orderItems
                .Where(i => i.Order != null)
                .GroupBy(i => i.MedicineId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Order.Date).First());

            _allStock = stocks
                .Select(s =>
                {
                    var hasSupply = latestSupplyByMedicine.TryGetValue(s.MedicineId, out var latestSupply);
                    var provider = hasSupply ? latestSupply?.Order?.Provider : null;
                    var restockDate = hasSupply ? latestSupply?.Order?.Date : null;

                    return new StockRow(s, provider, restockDate, LowStockThreshold);
                })
                .ToList();

            UpdateList();
        }
    }

    public class StockRow
    {
        public Stock Source { get; }
        public Provider? Provider { get; }
        public DateTime? LastRestockDate { get; }
        public int MinimumStock { get; }

        public string MedicineName => Source.Medicine?.Name ?? "—";
        public string Price => $"{Source.Medicine?.Price ?? 0:0.00} ₴";
        public string Quantity => $"x{Source.Quantity}";
        public string MinimumStockText => $"x{MinimumStock}";
        public string ProviderName => Provider?.Name ?? "—";
        public string LastRestock => LastRestockDate?.ToString("dd.MM.yyyy") ?? "—";
        public bool HasProvider => Provider != null;
        public bool IsLowStock => Source.Quantity < MinimumStock;
        public string QuantityColor => IsLowStock ? "#E06C6C" : "#D0D0D0";
        public string StockState => IsLowStock ? "Низько" : "Норма";
        public string StockStateColor => IsLowStock ? "#7C4A4A" : "#4A7C59";

        public StockRow(Stock source, Provider? provider, DateTime? lastRestockDate, int minimumStock)
        {
            Source = source;
            Provider = provider;
            LastRestockDate = lastRestockDate;
            MinimumStock = minimumStock;
        }
    }
}
