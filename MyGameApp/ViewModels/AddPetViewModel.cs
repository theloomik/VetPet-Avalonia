using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private List<PetType> _allPetTypes = new();

        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string? _selectedGender;
        [ObservableProperty] private string? _selectedSpecies;
        [ObservableProperty] private string? _selectedBreed;
        [ObservableProperty] private string _newSpeciesName = "";
        [ObservableProperty] private string _newBreedName = "";
        [ObservableProperty] private bool _isAddingSpecies;
        [ObservableProperty] private bool _isAddingBreed;
        [ObservableProperty] private int? _selectedBirthDay;
        [ObservableProperty] private int? _selectedBirthMonth;
        [ObservableProperty] private int? _selectedBirthYear;
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<string> SpeciesOptions { get; } = new();
        public ObservableCollection<string> BreedOptions { get; } = new();
        public ObservableCollection<string> Genders { get; } = new() { "male", "female" };
        public ObservableCollection<int> BirthDays { get; } = new();
        public ObservableCollection<int> BirthMonths { get; } = new();
        public ObservableCollection<int> BirthYears { get; } = new();

        public AddPetViewModel(ClientDetailsViewModel parent)
        {
            _parent = parent;
            SeedDateOptions();
            _ = LoadPetTypesAsync();
            SelectedGender = Genders[0];
        }

        partial void OnSelectedSpeciesChanged(string? value)
        {
            ReloadBreedsForSpecies(value);
            IsAddingBreed = false;
            NewBreedName = string.Empty;
        }

        private void SeedDateOptions()
        {
            BirthDays.Clear();
            for (var i = 1; i <= 31; i++)
                BirthDays.Add(i);

            BirthMonths.Clear();
            for (var i = 1; i <= 12; i++)
                BirthMonths.Add(i);

            BirthYears.Clear();
            for (var year = 2026; year >= 1950; year--)
                BirthYears.Add(year);
        }

        private async Task LoadPetTypesAsync()
        {
            using var db = new VetpetContext();
            _allPetTypes = await db.PetTypes.AsNoTracking().ToListAsync();

            var species = _allPetTypes
                .Select(t => t.Species?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            SpeciesOptions.Clear();
            foreach (var s in species!)
                SpeciesOptions.Add(s!);

            if (SpeciesOptions.Count > 0)
                SelectedSpecies = SpeciesOptions[0];
            else
                ReloadBreedsForSpecies(null);
        }

        private void ReloadBreedsForSpecies(string? species)
        {
            BreedOptions.Clear();
            SelectedBreed = null;

            if (string.IsNullOrWhiteSpace(species))
                return;

            BreedOptions.Add("(без породи)");

            var breeds = _allPetTypes
                .Where(t => string.Equals(t.Species, species, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Breed?.Trim())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(b => b)
                .ToList();

            foreach (var breed in breeds!)
                BreedOptions.Add(breed!);

            SelectedBreed = BreedOptions[0];
        }

        [RelayCommand]
        private void ShowAddSpecies()
        {
            NewSpeciesName = string.Empty;
            ErrorMessage = null;
            IsAddingSpecies = true;
        }

        [RelayCommand]
        private void CancelAddSpecies()
        {
            IsAddingSpecies = false;
            NewSpeciesName = string.Empty;
        }

        [RelayCommand]
        private void SaveSpecies()
        {
            var newSpecies = NewSpeciesName?.Trim();
            if (string.IsNullOrWhiteSpace(newSpecies))
            {
                ErrorMessage = "Вкажіть назву виду";
                return;
            }

            var existing = SpeciesOptions.FirstOrDefault(s => string.Equals(s, newSpecies, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                SpeciesOptions.Add(newSpecies);
                var ordered = SpeciesOptions.OrderBy(s => s).ToList();
                SpeciesOptions.Clear();
                foreach (var item in ordered)
                    SpeciesOptions.Add(item);
                SelectedSpecies = newSpecies;
            }
            else
            {
                SelectedSpecies = existing;
            }

            IsAddingSpecies = false;
            NewSpeciesName = string.Empty;
            ErrorMessage = null;
        }

        [RelayCommand]
        private void ShowAddBreed()
        {
            if (string.IsNullOrWhiteSpace(SelectedSpecies))
            {
                ErrorMessage = "Спочатку оберіть вид";
                return;
            }

            NewBreedName = string.Empty;
            ErrorMessage = null;
            IsAddingBreed = true;
        }

        [RelayCommand]
        private void CancelAddBreed()
        {
            IsAddingBreed = false;
            NewBreedName = string.Empty;
        }

        [RelayCommand]
        private void SaveBreed()
        {
            var newBreed = NewBreedName?.Trim();
            if (string.IsNullOrWhiteSpace(newBreed))
            {
                ErrorMessage = "Вкажіть назву породи";
                return;
            }

            var existing = BreedOptions.FirstOrDefault(b => string.Equals(b, newBreed, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                BreedOptions.Add(newBreed);
                var ordered = BreedOptions
                    .Where(b => b != "(без породи)")
                    .OrderBy(b => b)
                    .ToList();

                BreedOptions.Clear();
                BreedOptions.Add("(без породи)");
                foreach (var item in ordered)
                    BreedOptions.Add(item);
                SelectedBreed = newBreed;
            }
            else
            {
                SelectedBreed = existing;
            }

            IsAddingBreed = false;
            NewBreedName = string.Empty;
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Кличка є обов'язковою";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedSpecies))
            {
                ErrorMessage = "Оберіть вид тварини";
                return;
            }

            DateOnly? parsedDate = null;
            if (SelectedBirthDay.HasValue || SelectedBirthMonth.HasValue || SelectedBirthYear.HasValue)
            {
                if (!SelectedBirthDay.HasValue || !SelectedBirthMonth.HasValue || !SelectedBirthYear.HasValue)
                {
                    ErrorMessage = "Оберіть день, місяць і рік або залиште дату порожньою";
                    return;
                }

                if (!DateOnly.TryParse($"{SelectedBirthYear:0000}-{SelectedBirthMonth:00}-{SelectedBirthDay:00}", out var date))
                {
                    ErrorMessage = "Невірна дата народження";
                    return;
                }
                parsedDate = date;
            }

            var breed = string.Equals(SelectedBreed, "(без породи)", StringComparison.Ordinal)
                ? null
                : SelectedBreed?.Trim();

            using var db = new VetpetContext();
            var species = SelectedSpecies.Trim();

            var petType = await db.PetTypes.FirstOrDefaultAsync(t =>
                t.Species == species &&
                ((t.Breed == null && breed == null) || t.Breed == breed));

            if (petType == null)
            {
                petType = new PetType
                {
                    Species = species,
                    Breed = string.IsNullOrWhiteSpace(breed) ? null : breed
                };
                db.PetTypes.Add(petType);
                await db.SaveChangesAsync();
            }

            var pet = new Pet
            {
                ClientId = _parent.SelectedClient.Id,
                PetTypeId = petType.Id,
                Name = Name.Trim(),
                BirthDate = parsedDate,
                Gender = string.IsNullOrWhiteSpace(SelectedGender) ? null : SelectedGender
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