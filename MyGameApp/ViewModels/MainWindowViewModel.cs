using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MyGameApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel = null!;
        private INotifyPropertyChanged? _currentViewModelNotifier;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (!SetProperty(ref _currentViewModel, value))
                {
                    return;
                }

                SubscribeToCurrentViewModel(value);
                UpdateChildModalState();
            }
        }

        [ObservableProperty] private bool _isQuickAppointmentOpen;
        [ObservableProperty] private QuickAppointmentViewModel? _quickAppointmentForm;
        [ObservableProperty] private bool _isChildModalOpen;

        public bool IsAnyRightModalOpen => IsQuickAppointmentOpen || IsChildModalOpen;

        public MainWindowViewModel()
        {
            CurrentViewModel = new ClientsViewModel(this);
        }

        public void ChangeTab(int index)
        {
            CurrentViewModel = index switch
            {
                0 => new ClientsViewModel(this),
                1 => new AppointmentsViewModel(this),
                2 => new StaffViewModel(this),
                3 => new ProvidersViewModel(this),
                4 => new StockViewModel(this),
                _ => new ClientsViewModel(this)
            };
        }

        partial void OnIsQuickAppointmentOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyRightModalOpen));
        partial void OnIsChildModalOpenChanged(bool value) => OnPropertyChanged(nameof(IsAnyRightModalOpen));

        [RelayCommand]
        public void OpenQuickAppointment()
        {
            QuickAppointmentForm = new QuickAppointmentViewModel(this);
            IsQuickAppointmentOpen = true;
        }

        public void CloseQuickAppointment()
        {
            IsQuickAppointmentOpen = false;
            QuickAppointmentForm = null;
        }

        public async Task HandleQuickAppointmentSavedAsync(int clientId)
        {
            CloseQuickAppointment();

            switch (CurrentViewModel)
            {
                case AppointmentsViewModel appointments:
                    await appointments.ReloadAsync();
                    break;
                case ClientsViewModel clients:
                    await clients.ReloadAsync();
                    break;
                case ClientDetailsViewModel details when details.SelectedClient.Id == clientId:
                    await details.LoadAllAsync();
                    break;
            }
        }

        [RelayCommand]
        public void OpenClientDetails(MyGameApp.Models.Client? client)
        {
            if (client == null || client.Id <= 0)
                return;

            CurrentViewModel = new ClientDetailsViewModel(client, this);
        }

        [RelayCommand]
        public void OpenStaffDetails(MyGameApp.Models.Staff? staff)
        {
            if (staff == null || staff.Id <= 0)
                return;

            CurrentViewModel = new StaffDetailsViewModel(staff, this);
        }

        [RelayCommand]
        public void OpenProviderDetails(MyGameApp.Models.Provider? provider)
        {
            if (provider == null || provider.Id <= 0)
                return;

            CurrentViewModel = new ProviderDetailsViewModel(provider, this);
        }

        private void SubscribeToCurrentViewModel(ViewModelBase viewModel)
        {
            if (_currentViewModelNotifier != null)
            {
                _currentViewModelNotifier.PropertyChanged -= OnCurrentViewModelPropertyChanged;
            }

            _currentViewModelNotifier = viewModel;
            _currentViewModelNotifier.PropertyChanged += OnCurrentViewModelPropertyChanged;
        }

        private void OnCurrentViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName.EndsWith("Open") || e.PropertyName == "IsAnyModalOpen")
            {
                UpdateChildModalState();
            }
        }

        private void UpdateChildModalState()
        {
            IsChildModalOpen = CurrentViewModel switch
            {
                ClientsViewModel vm => vm.IsAddOpen,
                StaffViewModel vm => vm.IsAddOpen,
                ProvidersViewModel vm => vm.IsAddOpen,
                StockViewModel vm => vm.IsAnyModalOpen,
                ClientDetailsViewModel vm => vm.IsAnyModalOpen,
                _ => false
            };
        }
    }
}
