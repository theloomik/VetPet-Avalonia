using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class AddStockViewModel : ViewModelBase
    {
        private readonly StockViewModel _parent;

        [ObservableProperty] private string _medicineName = "";
        [ObservableProperty] private string _price = "";
        [ObservableProperty] private string _quantity = "1";
        [ObservableProperty] private Provider? _selectedProvider;
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<Provider> Providers { get; } = new();

        public AddStockViewModel(StockViewModel parent)
        {
            _parent = parent;
            _ = LoadProvidersAsync();
        }

        private async Task LoadProvidersAsync()
        {
            using var db = new VetpetContext();
            var list = await db.Providers.AsNoTracking().ToListAsync();
            Providers.Clear();
            foreach (var p in list)
                Providers.Add(p);

            SelectedProvider = Providers.Count > 0 ? Providers[0] : null;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(MedicineName) || string.IsNullOrWhiteSpace(Price))
            {
                ErrorMessage = "Назва та ціна є обов'язковими";
                return;
            }
            if (!decimal.TryParse(Price.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                ErrorMessage = "Невірний формат ціни";
                return;
            }
            if (!int.TryParse(Quantity, out var qty) || qty < 1)
            {
                ErrorMessage = "Кількість має бути цілим числом > 0";
                return;
            }
            if (SelectedProvider == null)
            {
                ErrorMessage = "Оберіть провайдера";
                return;
            }

            using var db = new VetpetContext();
            var medicine = new Medicine
            {
                Name = MedicineName.Trim(),
                Price = price
            };
            db.Medicines.Add(medicine);
            await db.SaveChangesAsync();

            var stock = new Stock
            {
                MedicineId = medicine.Id,
                Quantity = qty
            };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();

            var order = new ProviderOrder
            {
                ProviderId = SelectedProvider.Id,
                Date = DateTime.Now,
                TotalCost = price * qty,
                Status = "completed"
            };
            db.ProviderOrders.Add(order);
            await db.SaveChangesAsync();

            db.ProviderOrderItems.Add(new ProviderOrderItem
            {
                OrderId = order.Id,
                MedicineId = medicine.Id,
                Quantity = qty,
                Price = price
            });
            await db.SaveChangesAsync();

            await _parent.ReloadAsync();
            _parent.IsAddOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddOpen = false;
    }
}
