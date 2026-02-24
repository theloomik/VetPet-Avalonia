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
        [ObservableProperty] private string? _birthDate;
        [ObservableProperty] private string? _selectedGender;
        [ObservableProperty] private string? _selectedSpecies;
        [ObservableProperty] private string? _selectedBreed;
        [ObservableProperty] private string? _newSpeciesName;
        [ObservableProperty] private string? _errorMessage;

        public ObservableCollection<string> SpeciesOptions { get; } = new();
        public ObservableCollection<string> BreedOptions { get; } = new();
        public ObservableCollection<string> Genders { get; } = new() { "Самець", "Самка" };

        public AddPetViewModel(ClientDetailsViewModel parent)
        {
            _parent = parent;
            _ = LoadPetTypesAsync();
            SelectedGender = Genders[0];
        }

        partial void OnSelectedSpeciesChanged(string? value)
        {
            ReloadBreedsForSpecies(value);
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
        private async Task AddSpecies()
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

            NewSpeciesName = string.Empty;
            ErrorMessage = null;
            await Task.CompletedTask;
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
