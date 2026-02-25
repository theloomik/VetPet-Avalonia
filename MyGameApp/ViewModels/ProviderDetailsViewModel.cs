using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class ProviderDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? _mainVm;

        [ObservableProperty] private Provider _selectedProvider = null!;
        [ObservableProperty] private string? _loadError;
        [ObservableProperty] private int _ordersCount;
        [ObservableProperty] private int _medicinesCount;
        [ObservableProperty] private DateTime? _lastOrderDate;

        public string Phone => string.IsNullOrWhiteSpace(SelectedProvider.Phone) ? "—" : SelectedProvider.Phone;
        public string ContactPerson => string.IsNullOrWhiteSpace(SelectedProvider.ContactPerson) ? "—" : SelectedProvider.ContactPerson!;
        public string Email => string.IsNullOrWhiteSpace(SelectedProvider.Email) ? "—" : SelectedProvider.Email!;
        public string Address => string.IsNullOrWhiteSpace(SelectedProvider.Address) ? "—" : SelectedProvider.Address!;
        public string LastOrder => LastOrderDate?.ToString("dd.MM.yyyy HH:mm") ?? "—";

        public ProviderDetailsViewModel(Provider? provider = null, MainWindowViewModel? mainVm = null)
        {
            _mainVm = mainVm;
            SelectedProvider = provider ?? new Provider();

            if (SelectedProvider.Id > 0)
                _ = LoadAsync();
        }

        [RelayCommand]
        private void GoBack()
        {
            if (_mainVm == null)
                return;

            _mainVm.CurrentViewModel = new ProvidersViewModel(_mainVm);
        }

        private async Task LoadAsync()
        {
            try
            {
                using var db = new VetpetContext();

                var provider = await db.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == SelectedProvider.Id);

                if (provider == null)
                {
                    LoadError = "Provider not found";
                    return;
                }

                SelectedProvider = provider;

                OrdersCount = await db.ProviderOrders
                    .AsNoTracking()
                    .CountAsync(o => o.ProviderId == provider.Id);

                var orderIds = await db.ProviderOrders
                    .Where(o => o.ProviderId == provider.Id)
                    .Select(o => o.Id)
                    .ToListAsync();

                MedicinesCount = await db.ProviderOrderItems
                    .Where(i => orderIds.Contains(i.OrderId))
                    .Select(i => i.MedicineId)
                    .Distinct()
                    .CountAsync();

                LastOrderDate = await db.ProviderOrders
                    .Where(o => o.ProviderId == provider.Id)
                    .OrderByDescending(o => o.Date)
                    .Select(o => (DateTime?)o.Date)
                    .FirstOrDefaultAsync();

                LoadError = null;
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(ContactPerson));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Address));
                OnPropertyChanged(nameof(LastOrder));
            }
            catch (Exception ex)
            {
                LoadError = $"Failed to load provider: {ex.Message}";
            }
        }
    }
}
