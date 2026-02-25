using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace MyGameApp.ViewModels
{
    public partial class EditStockItemViewModel : ViewModelBase
    {
        private readonly StockViewModel _parent;
        private readonly int _stockId;
        private readonly int _medicineId;

        [ObservableProperty] private string _medicineName = "";
        [ObservableProperty] private string _price = "";
        [ObservableProperty] private string _quantity = "1";
        [ObservableProperty] private string? _errorMessage;

        public EditStockItemViewModel(StockViewModel parent, StockRow row)
        {
            _parent = parent;
            _stockId = row.Source.Id;
            _medicineId = row.Source.MedicineId;
            MedicineName = row.Source.Medicine?.Name ?? "";
            Price = $"{row.Source.Medicine?.Price ?? 0:0.00}";
            Quantity = row.Source.Quantity.ToString();
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(MedicineName) || string.IsNullOrWhiteSpace(Price))
            {
                ErrorMessage = "Name and price are required";
                return;
            }

            if (!decimal.TryParse(Price.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var price))
            {
                ErrorMessage = "Invalid price format";
                return;
            }

            if (!int.TryParse(Quantity, out var quantity) || quantity < 1)
            {
                ErrorMessage = "Quantity must be an integer > 0";
                return;
            }

            using var db = new Models.VetpetContext();
            var stock = await db.Stocks.FirstOrDefaultAsync(s => s.Id == _stockId);
            var medicine = await db.Medicines.FirstOrDefaultAsync(m => m.Id == _medicineId);
            if (stock == null || medicine == null)
            {
                ErrorMessage = "Stock item not found";
                return;
            }

            medicine.Name = MedicineName.Trim();
            medicine.Price = price;
            stock.Quantity = quantity;

            await db.SaveChangesAsync();
            await _parent.ReloadAsync();
            _parent.IsEditOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsEditOpen = false;
    }
}
