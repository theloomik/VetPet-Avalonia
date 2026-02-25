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
    public partial class AddBillViewModel : ViewModelBase
    {
        private readonly ClientDetailsViewModel _parent;

        [ObservableProperty] private Appointment? _selectedAppointment;
        [ObservableProperty] private string _totalAmount = "";
        [ObservableProperty] private string _dateText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        [ObservableProperty] private string _selectedPaid = "без оплати";
        [ObservableProperty] private string _paymentMethod = "";
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<Appointment> AppointmentOptions { get; } = new();
        public ObservableCollection<string> PaidOptions { get; } = new() { "без оплати", "оплачено" };

        public AddBillViewModel(ClientDetailsViewModel parent)
        {
            _parent = parent;
            _ = LoadOptionsAsync();
        }

        private async Task LoadOptionsAsync()
        {
            using var db = new VetpetContext();
            var appointments = await db.Appointments
                .Where(a => a.ClientId == _parent.SelectedClient.Id)
                .Include(a => a.Pet)
                .OrderByDescending(a => a.Date)
                .AsNoTracking()
                .ToListAsync();

            AppointmentOptions.Clear();
            foreach (var a in appointments) AppointmentOptions.Add(a);

            SelectedAppointment = AppointmentOptions.FirstOrDefault();
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            if (SelectedAppointment == null)
            {
                ErrorMessage = "Оберіть запис на прийом";
                return;
            }

            if (!decimal.TryParse(TotalAmount.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
            {
                ErrorMessage = "Вкажіть коректну суму";
                return;
            }

            if (!DateTime.TryParseExact(DateText.Trim(), "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                ErrorMessage = "Вкажіть дату у форматі DD.MM.YYYY HH:MM";
                return;
            }

            using var db = new VetpetContext();
            db.Bills.Add(new Bill
            {
                AppointmentId = SelectedAppointment.Id,
                TotalAmount = amount,
                Date = date,
                Paid = string.IsNullOrWhiteSpace(SelectedPaid) ? "без оплати" : SelectedPaid,
                PaymentMethod = string.IsNullOrWhiteSpace(PaymentMethod) ? null : PaymentMethod.Trim()
            });

            await db.SaveChangesAsync();
            await _parent.LoadBillsAsync();
            _parent.IsAddBillOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddBillOpen = false;
    }
}
