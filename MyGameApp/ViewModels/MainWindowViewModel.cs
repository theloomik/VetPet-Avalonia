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
        private ViewModelBase? _viewBeforeQuickAppointment;

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

        [ObservableProperty] private bool _isChildModalOpen;

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

        [RelayCommand]
        public void OpenQuickAppointment()
        {
            if (CurrentViewModel is QuickAppointmentViewModel)
            {
                return;
            }

            _viewBeforeQuickAppointment = CurrentViewModel;
            CurrentViewModel = new QuickAppointmentViewModel(this);
        }

        public void CloseQuickAppointment()
        {
            if (_viewBeforeQuickAppointment != null)
            {
                CurrentViewModel = _viewBeforeQuickAppointment;
                _viewBeforeQuickAppointment = null;
                return;
            }

            CurrentViewModel = new AppointmentsViewModel(this);
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
                case ClientDetailsViewModel:
                    CurrentViewModel = new AppointmentsViewModel(this);
                    break;
                default:
                    if (CurrentViewModel is not AppointmentsViewModel)
                    {
                        CurrentViewModel = new AppointmentsViewModel(this);
                    }
                    break;
            }
        }

        public bool TryCloseTopmostModal()
        {
            switch (CurrentViewModel)
            {
                case ClientDetailsViewModel vm:
                    if (vm.IsEditPetOpen)
                    {
                        vm.IsEditPetOpen = false;
                        return true;
                    }
                    if (vm.IsEditMode)
                    {
                        vm.IsEditMode = false;
                        return true;
                    }
                    if (vm.IsAddBillOpen)
                    {
                        vm.IsAddBillOpen = false;
                        return true;
                    }
                    if (vm.IsAddAppointmentOpen)
                    {
                        vm.IsAddAppointmentOpen = false;
                        return true;
                    }
                    if (vm.IsAddPetOpen)
                    {
                        vm.IsAddPetOpen = false;
                        return true;
                    }
                    return false;
                case QuickAppointmentViewModel vm:
                    if (vm.IsAddPetOpen)
                    {
                        vm.IsAddPetOpen = false;
                        return true;
                    }
                    if (vm.IsAddClientOpen)
                    {
                        vm.IsAddClientOpen = false;
                        return true;
                    }
                    return false;
                case StockViewModel vm:
                    if (vm.IsEditOpen)
                    {
                        vm.IsEditOpen = false;
                        return true;
                    }
                    if (vm.IsAddOpen)
                    {
                        vm.IsAddOpen = false;
                        return true;
                    }
                    return false;
                case StaffViewModel vm when vm.IsAddOpen:
                    vm.IsAddOpen = false;
                    return true;
                case ProvidersViewModel vm when vm.IsAddOpen:
                    vm.IsAddOpen = false;
                    return true;
                case ClientsViewModel vm when vm.IsAddOpen:
                    vm.IsAddOpen = false;
                    return true;
                default:
                    return false;
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
                QuickAppointmentViewModel vm => vm.IsAnyModalOpen,
                _ => false
            };
        }
    }
}
