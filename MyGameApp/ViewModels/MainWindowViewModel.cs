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
            CurrentViewModel = new ClientsViewModel();
        }

        public void ChangeTab(int index)
        {
            CurrentViewModel = index switch
            {
                0 => new ClientsViewModel(),
                1 => new AppointmentsViewModel(),
                2 => new StaffViewModel(),
                3 => new ProvidersViewModel(),
                4 => new StockViewModel(),
                _ => new ClientsViewModel()
            };
        }
    }
}