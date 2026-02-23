using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyGameApp.Models;

namespace MyGameApp.ViewModels
{
    public partial class AddPetViewModel : ViewModelBase
    {
        private readonly ClientDetailsViewModel _parent;

        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string? _birthDate;
        [ObservableProperty] private string? _gender;
        [ObservableProperty] private PetType? _selectedPetType;
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<PetType> PetTypes { get; } = new();

        public AddPetViewModel(ClientDetailsViewModel parent)
        {
            _parent = parent;
            _ = LoadPetTypesAsync();
        }

        private async Task LoadPetTypesAsync()
        {
            using var db = new VetpetContext();
            var types = await db.PetTypes.AsNoTracking().ToListAsync();
            PetTypes.Clear();
            foreach (var t in types) PetTypes.Add(t);
            if (PetTypes.Count > 0) SelectedPetType = PetTypes[0];
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Кличка є обов'язковою";
                return;
            }

            if (SelectedPetType == null)
            {
                ErrorMessage = "Оберіть вид тварини";
                return;
            }

            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(BirthDate))
            {
                if (DateOnly.TryParse(BirthDate, out var d))
                    parsedDate = d;
                else
                {
                    ErrorMessage = "Невірний формат дати (рррр-мм-дд)";
                    return;
                }
            }

            using var db = new VetpetContext();
            var pet = new Pet
            {
                ClientId  = _parent.SelectedClient.Id,
                PetTypeId = SelectedPetType.Id,
                Name      = Name.Trim(),
                BirthDate = parsedDate,
                Gender    = string.IsNullOrWhiteSpace(Gender) ? null : Gender.Trim()
            };
            db.Pets.Add(pet);
            await db.SaveChangesAsync();
            await _parent.LoadPetsAsync();
            _parent.IsAddPetOpen = false;
        }

        [RelayCommand]
        private void Cancel() => _parent.IsAddPetOpen = false;
    }
}
