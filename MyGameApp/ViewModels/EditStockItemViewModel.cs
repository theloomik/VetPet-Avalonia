using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class EditStockItemViewModel : ViewModelBase
    {
        private readonly StockViewModel _parent;
        private readonly int _stockId;
        private readonly int _medicineId;
        private readonly int _initialQuantity;
        private readonly int? _initialProviderId;

        [ObservableProperty] private string _medicineName = "";
        [ObservableProperty] private string _price = "";
        [ObservableProperty] private string _quantity = "1";
        [ObservableProperty] private string _minimumStock = "10";
        [ObservableProperty] private Provider? _selectedProvider;
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<Provider> Providers { get; } = new();

        public EditStockItemViewModel(StockViewModel parent, StockRow row)
        {
            _parent = parent;
            _stockId = row.Source.Id;
            _medicineId = row.Source.MedicineId;
            _initialQuantity = row.Source.Quantity;
            _initialProviderId = row.ProviderId;

            MedicineName = row.Source.Medicine?.Name ?? "";
            Price = $"{row.Source.Medicine?.Price ?? 0:0.00}";
            Quantity = row.Source.Quantity.ToString();
            MinimumStock = row.MinimumStock.ToString();

            _ = LoadProvidersAsync();
        }

        private async Task LoadProvidersAsync()
        {
            using var db = new VetpetContext();
            var list = await db.Providers.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

            Providers.Clear();
            foreach (var provider in list)
            {
                Providers.Add(provider);
            }

            SelectedProvider = _initialProviderId.HasValue
                ? Providers.FirstOrDefault(p => p.Id == _initialProviderId.Value)
                : Providers.FirstOrDefault();
        }

        [RelayCommand]
        private void IncreaseQuantity()
        {
            var current = ParseQuantityOrDefault();
            Quantity = (current + 1).ToString();
        }

        [RelayCommand]
        private void DecreaseQuantity()
        {
            var current = ParseQuantityOrDefault();
            Quantity = Math.Max(1, current - 1).ToString();
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(MedicineName) || string.IsNullOrWhiteSpace(Price))
            {
                ErrorMessage = "Назва та ціна є обов'язковими";
                return;
            }

            if (!decimal.TryParse(Price.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                ErrorMessage = "Невірний формат ціни";
                return;
            }

            if (!int.TryParse(Quantity, out var quantity) || quantity < 1)
            {
                ErrorMessage = "Кількість має бути цілим числом > 0";
                return;
            }

            if (!int.TryParse(MinimumStock, out var minimumStockValue) || minimumStockValue < 1)
            {
                ErrorMessage = "Мінімальний залишок має бути цілим числом > 0";
                return;
            }

            using var db = new VetpetContext();
            await using var tx = await db.Database.BeginTransactionAsync();

            var stock = await db.Stocks.FirstOrDefaultAsync(s => s.Id == _stockId);
            var medicine = await db.Medicines.FirstOrDefaultAsync(m => m.Id == _medicineId);
            if (stock == null || medicine == null)
            {
                ErrorMessage = "Позицію складу не знайдено";
                return;
            }

            medicine.Name = MedicineName.Trim();
            medicine.Price = price;
            var previousQuantity = stock.Quantity;
            stock.Quantity = quantity;

            await db.SaveChangesAsync();

            if (SelectedProvider != null)
            {
                _parent.SetProviderOverrideForMedicine(_medicineId, SelectedProvider.Id);

                var delta = quantity - previousQuantity;
                var providerChanged = _initialProviderId != SelectedProvider.Id;
                if (delta > 0 || providerChanged)
                {
                    var recordedQuantity = Math.Max(delta, 0);
                    var order = new ProviderOrder
                    {
                        ProviderId = SelectedProvider.Id,
                        Date = DateTime.Now,
                        TotalCost = price * recordedQuantity,
                        Status = "доставлено"
                    };
                    db.ProviderOrders.Add(order);
                    await db.SaveChangesAsync();

                    db.ProviderOrderItems.Add(new ProviderOrderItem
                    {
                        OrderId = order.Id,
                        MedicineId = _medicineId,
                        Quantity = recordedQuantity,
                        Price = price
                    });

                    await db.SaveChangesAsync();
                }
            }

            _parent.SetMinimumStockForMedicine(_medicineId, minimumStockValue);

            await tx.CommitAsync();
            await _parent.ReloadAsync();
            _parent.IsEditOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsEditOpen = false;

        private int ParseQuantityOrDefault()
        {
            return int.TryParse(Quantity, out var quantity) && quantity > 0 ? quantity : _initialQuantity;
        }
    }
}
