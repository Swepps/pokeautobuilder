using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Accord.IO;
using ApexCharts;
using AutoBuilder;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.VisualBasic;
using PokeApiNet;
using PokemonDataModel;
using Utility;
using static MudBlazor.Colors;
using static PokeAutobuilder.Pages.TeamBuilderPage;

namespace PokeAutobuilder.Source.Services
{
    public record Preferences
    {
        public bool DarkMode { get; init; }
        public bool AllowMultipleMegas { get; init; }
        public bool AllowMultipleGmax { get; init; }
    }

    public class ProfileService(
        ILocalStorageService localStorageService,
        IApexChartService apexChartService
    )
    {
        private readonly ILocalStorageService _localStorageService = localStorageService;
        private readonly IApexChartService _apexChartService = apexChartService;

        public event Action? OnStorageChange;
        public event Action? OnTeamStorageChange;
        public event Action? OnPreferencesChange;

        // preferences
        private Preferences _preferences = new()
        {
            DarkMode = true,
            AllowMultipleMegas = false,
            AllowMultipleGmax = false,
        };

        private void NotifyPrefsChanged() => OnPreferencesChange?.Invoke();

        public bool IsDarkMode
        {
            get => _preferences.DarkMode;
            set
            {
                _preferences = _preferences with { DarkMode = value };
                NotifyPrefsChanged();
                _ = UpdatePreferencesAsync();
                _ = _apexChartService.SetGlobalOptionsAsync(
                    new ApexChartBaseOptions()
                    {
                        Theme = new Theme
                        {
                            Palette = ApexCharts.PaletteType.Palette7,
                            Mode = value ? ApexCharts.Mode.Dark : ApexCharts.Mode.Light,
                        },
                    },
                    true
                );
            }
        }

        public bool AllowMultipleMegas
        {
            get => _preferences.AllowMultipleMegas;
            set
            {
                _preferences = _preferences with { AllowMultipleMegas = value };
                NotifyPrefsChanged();
                _ = UpdatePreferencesAsync();
            }
        }

        public bool AllowMultipleGmax
        {
            get => _preferences.AllowMultipleGmax;
            set
            {
                _preferences = _preferences with { AllowMultipleGmax = value };
                NotifyPrefsChanged();
                _ = UpdatePreferencesAsync();
            }
        }

        // global variables
        private static readonly string PREFERENCES_KEY = "preferences";
        private static readonly string VERSION_KEY = "version";
        private static readonly string POKEMON_TYPES_KEY = "pokemon_types";
        private static readonly string POKEMON_STORAGE_KEY = "pokemon_storage";
        private static readonly string TEAM_STORAGE_KEY = "pokemon_team_storage";
        private static readonly string AUTOBUILDER_PARAMS = "autobuilder_params";

        private List<PokeApiNet.Type> _allTypes = [];
        public List<PokeApiNet.Type> AllTypes
        {
            get => _allTypes;
            set { _ = SetAllTypesAsync(value); }
        }

        private PokemonStorage _pokemonStorage = new();
        public PokemonStorage PokemonStorage
        {
            get => _pokemonStorage;
            set
            {
                _pokemonStorage = value;
                _ = SetPokemonStorageAsync(value);
            }
        }

        private List<PokemonTeam> _teamStorage = [];
        public List<PokemonTeam> TeamStorage
        {
            get => _teamStorage;
            set
            {
                _teamStorage = value;
                _ = SetTeamStorageAsync(value);
            }
        }

        private AutoBuilderWeightings _autoBuilderParams = new();
        public AutoBuilderWeightings AutoBuilderParams
        {
            get => _autoBuilderParams;
            set
            {
                _autoBuilderParams = value;
                _ = SetAutoBuilderParamsAsync(value);
            }
        }

        public async Task LoadProfileStorage()
        {
            try
            {
                double profileStorageVersion = await GetVersionAsync();

                // we want to refresh the cached types each time there is a version update to ensure
                // the types are fresh from the API
                if (profileStorageVersion == Globals.Version)
                {
                    _allTypes = await _localStorageService.GetItemAsync<List<PokeApiNet.Type>>(
                        POKEMON_TYPES_KEY
                    )
                        is { } allTypes
                        ? allTypes
                        : [];
                }

                // if profile doesn't contain pokemon types, generate them
                if (AllTypes.Count == 0)
                {
                    AllTypes = await PokeApiService.Instance!.GetAllTypesAsync();
                }
                DataModelCache.LoadedTypes = AllTypes;

                // load pokemon storage
                if (profileStorageVersion <= 1.3)
                {
                    PokemonBox? box = await _localStorageService.GetItemAsync<PokemonBox>(
                        POKEMON_STORAGE_KEY
                    );
                    if (box is not null)
                    {
                        _pokemonStorage.Boxes.Add(box);
                    }
                    else if (_pokemonStorage.Boxes.Count == 0)
                    {
                        _pokemonStorage.Boxes.Add(new PokemonBox("Box 1"));
                    }

                    await SetPokemonStorageAsync(_pokemonStorage);
                    await UpdateVersionAsync();
                }
                else
                {
                    await LoadPokemonStorageAsync();
                }

                // load team storage
                _teamStorage = await _localStorageService.GetItemAsync<List<PokemonTeam>>(
                    TEAM_STORAGE_KEY
                )
                    is { } teamStorage
                    ? teamStorage
                    : [];

                // load autobuilder params
                _autoBuilderParams = await _localStorageService.GetItemAsync<AutoBuilderWeightings>(
                    AUTOBUILDER_PARAMS
                )
                    is { } weightings
                    ? weightings
                    : new(
                        AutoBuilderWeightings.MakeDefaultTypeWeightings(),
                        resistanceAll: 0.5,
                        resistanceBalance: 0.5,
                        resistanceAmount: 0.5,
                        weaknessBalance: 0.5,
                        weaknessAmount: 0.5,
                        stabAll: 0.5,
                        stabBalance: 0.5,
                        stabAmount: 0.5,
                        moveSetAll: 0.5,
                        moveSetBalance: 0.5,
                        moveSetAmount: 0.5,
                        coverageOnOffensive: 0.0,
                        resistancesOnDefensive: 0.0,
                        baseStatTotal: 0.5,
                        baseStatHp: 0.5,
                        baseStatAtt: 0.5,
                        baseStatDef: 0.5,
                        baseStatSpAtt: 0.5,
                        baseStatSpDef: 0.5,
                        baseStatSpe: 0.5
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                // if loading storage fails we'll just have to reset it so users don't get stuck
                // * Only in RELEASE
#if DEBUG

#else
                await _localStorageService.ClearAsync();
#endif
            }
        }

        public async Task<double> GetVersionAsync()
        {
            if (await _localStorageService.ContainKeyAsync(VERSION_KEY))
                return await _localStorageService.GetItemAsync<double>(VERSION_KEY);

            return Globals.Version;
        }

        public async Task UpdateVersionAsync()
        {
            // store the current version in the cache
            await _localStorageService.SetItemAsync(VERSION_KEY, Globals.Version);
        }

        // --- Preferences ---
        public async Task<Preferences> GetPreferencesAsync()
        {
            // if they've already specified their preferences explicitly, use them
            if (await _localStorageService.ContainKeyAsync(PREFERENCES_KEY))
            {
                _preferences = (
                    await _localStorageService.GetItemAsync<Preferences>(PREFERENCES_KEY)
                )!;
            }

            return _preferences;
        }

        public async Task UpdatePreferencesAsync()
        {
            await _localStorageService.SetItemAsync(PREFERENCES_KEY, _preferences);
        }

        public async Task SetAllTypesAsync(List<PokeApiNet.Type> allTypes)
        {
            _allTypes = allTypes;
            await _localStorageService.SetItemAsync(POKEMON_TYPES_KEY, allTypes);
        }

        public async Task LoadPokemonStorageAsync()
        {
            _pokemonStorage = await _localStorageService.GetItemAsync<PokemonStorage>(
                POKEMON_STORAGE_KEY
            )
                is { } pokemonStorage
                ? pokemonStorage
                : new();
            if (_pokemonStorage.Boxes.Count == 0)
            {
                _pokemonStorage.Boxes.Add(new PokemonBox("Box 1"));
            }
        }

        public async Task SetPokemonStorageAsync(PokemonStorage storage)
        {
            OnStorageChange?.Invoke();
            await _localStorageService.SetItemAsync(POKEMON_STORAGE_KEY, storage);
        }

        public async Task UpdatePokemonStorageAsync()
        {
            await SetPokemonStorageAsync(PokemonStorage);
        }

        public async Task AddPokemonToStorageAsync(SmartPokemon pokemon, int boxIdx)
        {
            if (boxIdx < 0 || boxIdx >= PokemonStorage.Boxes.Count)
            {
                throw new IndexOutOfRangeException(
                    $"Box with index {boxIdx} does not exist in storage."
                );
            }

            PokemonStorage.Boxes[boxIdx].Pokemon.Add(pokemon);
            await UpdatePokemonStorageAsync();
        }

        public PokemonBox? FindPokemonBoxForPokemon(SmartPokemon pokemon)
        {
            return PokemonStorage
                .Boxes.Where(box => box.Pokemon.Contains(pokemon))
                .FirstOrDefault();
        }

        public async Task<bool> RemovePokemonFromStorageAsync(SmartPokemon pokemon)
        {
            PokemonBox? owningBox = FindPokemonBoxForPokemon(pokemon);
            if (owningBox is null)
                return false;

            bool removed = owningBox.Pokemon.Remove(pokemon);
            if (removed)
                await UpdatePokemonStorageAsync();

            return removed;
        }

        public async Task<bool> ReplacePokemonInStorageAsync(
            SmartPokemon oldPokemon,
            SmartPokemon newPokemon
        )
        {
            PokemonBox? owningBox = FindPokemonBoxForPokemon(oldPokemon);
            if (owningBox is null)
                return false;

            int pokemonIdx = owningBox.Pokemon.IndexOf(oldPokemon);

            if (pokemonIdx < 0)
                return false;

            owningBox.Pokemon.RemoveAt(pokemonIdx);
            owningBox.Pokemon.Insert(pokemonIdx, newPokemon);

            await UpdatePokemonStorageAsync();
            return true;
        }

        public async Task SetTeamStorageAsync(List<PokemonTeam> teamStorage)
        {
            OnTeamStorageChange?.Invoke();
            await _localStorageService.SetItemAsync(TEAM_STORAGE_KEY, teamStorage);
        }

        public async Task AddTeamToStorageAsync(PokemonTeam team)
        {
            if (TeamStorage == null)
                return;

            TeamStorage.Add(new PokemonTeam(team));
            await SetTeamStorageAsync(TeamStorage);
        }

        public async Task<bool> RemoveTeamFromStorageAsync(PokemonTeam team)
        {
            if (TeamStorage == null)
                return false;

            bool removed = TeamStorage.Remove(team);
            if (removed)
                await SetTeamStorageAsync(TeamStorage);

            return removed;
        }

        public async Task SetAutoBuilderParamsAsync(AutoBuilderWeightings weightings)
        {
            _autoBuilderParams = weightings;
            await _localStorageService.SetItemAsync(AUTOBUILDER_PARAMS, weightings);
        }
    }
}
