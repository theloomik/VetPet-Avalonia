using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
            {
                Providers.Add(p);
            }

            SelectedProvider = Providers.Count > 0 ? Providers[0] : null;
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

            try
            {
                using var db = new VetpetContext();
                await using var tx = await db.Database.BeginTransactionAsync();

                var medicineName = MedicineName.Trim();
                var medicine = await db.Medicines
                    .FirstOrDefaultAsync(m => m.Name.ToLower() == medicineName.ToLower());

                if (medicine == null)
                {
                    medicine = new Medicine
                    {
                        Name = medicineName,
                        Price = price
                    };
                    db.Medicines.Add(medicine);
                    await db.SaveChangesAsync();
                }
                else
                {
                    medicine.Price = price;
                    await db.SaveChangesAsync();
                }

                var stock = await db.Stocks.FirstOrDefaultAsync(s => s.MedicineId == medicine.Id);
                if (stock == null)
                {
                    stock = new Stock
                    {
                        MedicineId = medicine.Id,
                        Quantity = qty
                    };
                    db.Stocks.Add(stock);
                }
                else
                {
                    stock.Quantity += qty;
                }
                await db.SaveChangesAsync();

                var order = new ProviderOrder
                {
                    ProviderId = SelectedProvider.Id,
                    Date = DateTime.Now,
                    TotalCost = price * qty,
                    Status = "доставлено"
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

                await tx.CommitAsync();
            }
            catch
            {
                ErrorMessage = "Не вдалося зберегти препарат. Перевірте дані та підключення до БД.";
                return;
            }

            await _parent.ReloadAsync();
            _parent.IsAddOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddOpen = false;
    }
}
