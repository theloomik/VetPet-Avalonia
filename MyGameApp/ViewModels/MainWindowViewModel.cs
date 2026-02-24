using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyGameApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel = null!;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public MainWindowViewModel()
        {
            CurrentViewModel = new ClientsViewModel(this);
        }

        public void ChangeTab(int index)
        {
            CurrentViewModel = index switch
            {
                0 => new ClientsViewModel(this),
                1 => new AppointmentsViewModel(),
                2 => new StaffViewModel(),
                3 => new ProvidersViewModel(),
                4 => new StockViewModel(),
                _ => new ClientsViewModel(this)
            };
        }

        [RelayCommand]
        public void OpenClientDetails(MyGameApp.Models.Client? client)
        {
            if (client == null || client.Id == 0) return;
            CurrentViewModel = new ClientDetailsViewModel(client, this);
        }
    }
}