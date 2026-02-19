using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class AddClientViewModel : ViewModelBase
    {
        private readonly ClientsViewModel _parent;

        [ObservableProperty] private string _firstName = "";
        [ObservableProperty] private string _lastName = "";
        [ObservableProperty] private string _phone = "";
        [ObservableProperty] private string _email = "";
        [ObservableProperty] private string? _errorMessage;

        public AddClientViewModel(ClientsViewModel parent)
        {
            _parent = parent;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Phone))
            {
                ErrorMessage = "Прізвище, ім'я та телефон є обов'язковими";
                return;
            }

            using var db = new VetpetContext();
            var client = new Client
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Phone = Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            await _parent.ReloadAsync();
            _parent.IsAddOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddOpen = false;
    }
}
