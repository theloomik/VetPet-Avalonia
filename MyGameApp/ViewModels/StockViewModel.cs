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
        private const int DefaultLowStockThreshold = 10;

        private readonly MainWindowViewModel? _mainVm;
        private readonly Dictionary<int, int> _minimumStockByMedicine = new();
        private readonly Dictionary<int, int> _providerOverrideByMedicine = new();
        private List<StockRow> _allStock = new();
        private StockSortField _sortField = StockSortField.Medicine;
        private bool _sortAsc = true;

        public ObservableCollection<StockRow> Stocks { get; } = new();

        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isAddOpen;
        [ObservableProperty] private AddStockViewModel? _addForm;
        [ObservableProperty] private bool _isEditOpen;
        [ObservableProperty] private EditStockItemViewModel? _editForm;
        [ObservableProperty] private int _totalPositions;
        [ObservableProperty] private int _lowStockPositions;
        [ObservableProperty] private int _suppliersCount;

        public bool IsAnyModalOpen => IsAddOpen || IsEditOpen;
        public string MedicineSortLabel => $"ПРЕПАРАТ {GetSortArrow(StockSortField.Medicine)}";
        public string PriceSortLabel => $"ЦІНА {GetSortArrow(StockSortField.Price)}";
        public string QuantitySortLabel => $"КІЛЬКІСТЬ {GetSortArrow(StockSortField.Quantity)}";

        public IRelayCommand SortByMedicineCommand { get; }
        public IRelayCommand SortByPriceCommand { get; }
        public IRelayCommand SortByQuantityCommand { get; }
        public IRelayCommand AddStockCommand { get; }
        public IRelayCommand<StockRow?> GoToProviderCommand { get; }
        public IRelayCommand<StockRow?> OpenEditCommand { get; }

        public StockViewModel(MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            SortByMedicineCommand = new RelayCommand(() => ApplySort(StockSortField.Medicine));
            SortByPriceCommand = new RelayCommand(() => ApplySort(StockSortField.Price));
            SortByQuantityCommand = new RelayCommand(() => ApplySort(StockSortField.Quantity));
            AddStockCommand = new RelayCommand(OpenAdd);
            GoToProviderCommand = new RelayCommand<StockRow?>(GoToProvider);
            OpenEditCommand = new RelayCommand<StockRow?>(OpenEdit);
            _ = ReloadAsync();
        }

        partial void OnSearchTextChanged(string value) => UpdateList();
        partial void OnIsAddOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));
        partial void OnIsEditOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyModalOpen));

        public void SetMinimumStockForMedicine(int medicineId, int minimumStock)
        {
            _minimumStockByMedicine[medicineId] = Math.Max(1, minimumStock);
        }

        public void SetProviderOverrideForMedicine(int medicineId, int providerId)
        {
            _providerOverrideByMedicine[medicineId] = providerId;
        }

        private void ApplySort(StockSortField field)
        {
            if (_sortField == field)
            {
                _sortAsc = !_sortAsc;
            }
            else
            {
                _sortField = field;
                _sortAsc = true;
            }

            OnPropertyChanged(nameof(MedicineSortLabel));
            OnPropertyChanged(nameof(PriceSortLabel));
            OnPropertyChanged(nameof(QuantitySortLabel));
            UpdateList();
        }

        private string GetSortArrow(StockSortField field)
        {
            if (_sortField != field)
            {
                return "";
            }

            return _sortAsc ? "↑" : "↓";
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

            q = ApplySorting(q);

            Stocks.Clear();
            foreach (var item in q)
            {
                Stocks.Add(item);
            }
        }

        private IEnumerable<StockRow> ApplySorting(IEnumerable<StockRow> source)
        {
            return (_sortField, _sortAsc) switch
            {
                (StockSortField.Price, true) => source.OrderBy(x => x.PriceValue).ThenBy(x => x.MedicineName),
                (StockSortField.Price, false) => source.OrderByDescending(x => x.PriceValue).ThenBy(x => x.MedicineName),
                (StockSortField.Quantity, true) => source.OrderBy(x => x.QuantityValue).ThenBy(x => x.MedicineName),
                (StockSortField.Quantity, false) => source.OrderByDescending(x => x.QuantityValue).ThenBy(x => x.MedicineName),
                (StockSortField.Medicine, true) => source.OrderBy(x => x.MedicineName),
                _ => source.OrderByDescending(x => x.MedicineName)
            };
        }

        public async Task ReloadAsync()
        {
            using var db = new VetpetContext();

            var stocks = await db.Stocks
                .Include(s => s.Medicine)
                .AsNoTracking()
                .ToListAsync();

            var providersById = await db.Providers
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id);

            var medicineIds = stocks.Select(s => s.MedicineId).Distinct().ToList();
            var orderItems = await db.ProviderOrderItems
                .Where(i => medicineIds.Contains(i.MedicineId))
                .Include(i => i.Order)
                .ThenInclude(o => o.Provider)
                .AsNoTracking()
                .ToListAsync();

            var latestProviderByMedicine = orderItems
                .Where(i => i.Order?.Provider != null)
                .GroupBy(i => i.MedicineId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.OrderId).First().Order?.Provider);

            var latestRestockDateByMedicine = orderItems
                .Where(i => i.Order != null)
                .GroupBy(i => i.MedicineId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(x => (DateTime?)x.Order!.Date));

            _allStock = stocks
                .Select(s =>
                {
                    var provider = latestProviderByMedicine.TryGetValue(s.MedicineId, out var latestProvider)
                        ? latestProvider
                        : null;

                    var restockDate = latestRestockDateByMedicine.TryGetValue(s.MedicineId, out var latestDate)
                        ? latestDate
                        : null;

                    if (_providerOverrideByMedicine.TryGetValue(s.MedicineId, out var providerId)
                        && providersById.TryGetValue(providerId, out var overrideProvider))
                    {
                        provider = overrideProvider;
                    }

                    var minimumStock = _minimumStockByMedicine.TryGetValue(s.MedicineId, out var customMinimum)
                        ? customMinimum
                        : DefaultLowStockThreshold;

                    _minimumStockByMedicine[s.MedicineId] = minimumStock;

                    return new StockRow(s, provider, restockDate, minimumStock);
                })
                .ToList();

            TotalPositions = _allStock.Count;
            LowStockPositions = _allStock.Count(x => x.IsLowStock);
            SuppliersCount = _allStock
                .Where(x => x.ProviderId.HasValue)
                .Select(x => x.ProviderId!.Value)
                .Distinct()
                .Count();

            UpdateList();
        }

        private enum StockSortField
        {
            Medicine,
            Price,
            Quantity
        }
    }

    public class StockRow
    {
        public Stock Source { get; }
        public Provider? Provider { get; }
        public DateTime? LastRestockDate { get; }
        public int MinimumStock { get; }

        public string MedicineName => Source.Medicine?.Name ?? "—";
        public string Price => $"{PriceValue:0.00} ₴";
        public decimal PriceValue => Source.Medicine?.Price ?? 0;
        public string Quantity => Source.Quantity.ToString();
        public int QuantityValue => Source.Quantity;
        public string MinimumStockText => MinimumStock.ToString();
        public string ProviderName => Provider?.Name ?? "—";
        public int? ProviderId => Provider?.Id;
        public string LastRestock => LastRestockDate?.ToString("dd.MM.yyyy") ?? "—";
        public bool HasProvider => Provider != null;
        public bool IsLowStock => Source.Quantity < MinimumStock;
        public string QuantityColor => IsLowStock ? "#FF8C8C" : "#D0D0D0";
        public string StockState => IsLowStock ? "Низько" : "Норма";
        public string StockStateColor => IsLowStock ? "#904646" : "#4A7C59";
        public string RowBackground => IsLowStock ? "#1FD35B5B" : "Transparent";
        public string RowBorderBrush => IsLowStock ? "#6CD35B5B" : "Transparent";
        public double RowBorderThickness => IsLowStock ? 1 : 0;

        public StockRow(Stock source, Provider? provider, DateTime? lastRestockDate, int minimumStock)
        {
            Source = source;
            Provider = provider;
            LastRestockDate = lastRestockDate;
            MinimumStock = minimumStock;
        }
    }
}
