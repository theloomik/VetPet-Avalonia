using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class AddProviderViewModel : ViewModelBase
    {
        private readonly ProvidersViewModel _parent;

        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _phone = "";
        [ObservableProperty] private string _email = "";
        [ObservableProperty] private string _address = "";
        [ObservableProperty] private string _contactPerson = "";
        [ObservableProperty] private string? _errorMessage;

        public AddProviderViewModel(ProvidersViewModel parent)
        {
            _parent = parent;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Phone))
            {
                ErrorMessage = "Назва та телефон є обов'язковими";
                return;
            }

            using var db = new VetpetContext();
            var provider = new Provider
            {
                Name = Name.Trim(),
                Phone = Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                ContactPerson = string.IsNullOrWhiteSpace(ContactPerson) ? null : ContactPerson.Trim()
            };
            db.Providers.Add(provider);
            await db.SaveChangesAsync();
            await _parent.ReloadAsync();
            _parent.IsAddOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddOpen = false;
    }
}
