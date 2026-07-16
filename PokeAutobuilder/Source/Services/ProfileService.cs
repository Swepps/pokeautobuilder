using ApexCharts;
using Autobuilder;
using Blazored.LocalStorage;
using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace PokeAutobuilder.Source.Services
{
    public record Preferences
    {
        public bool DarkMode { get; init; }
        public bool AllowMultipleMegas { get; init; }
        public bool AllowMultipleGmax { get; init; }
    }

    public class ProfileService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly IApexChartService _apexChartService;
        private readonly PokeApiService _apiService;
        private readonly TypeChart _typeChart;

        public event Action? OnStorageChange;
        public event Action? OnTeamStorageChange;
        public event Action? OnPreferencesChange;

        // global variables
        private static readonly string PREFERENCES_KEY = "preferences";
        private static readonly string VERSION_KEY = "version";
        private static readonly string POKEMON_TYPES_KEY = "pokemon_types";
        private static readonly string POKEMON_STORAGE_KEY = "pokemon_storage";
        private static readonly string TEAM_STORAGE_KEY = "pokemon_team_storage";
        private static readonly string AUTOBUILDER_PARAMS = "autobuilder_params";

        private readonly PersistedState<Preferences> _preferences;
        private readonly PersistedState<List<PokeApiNet.Type>> _allTypes;
        private readonly PersistedState<PokemonStorage> _pokemonStorage;
        private readonly PersistedState<List<PokemonTeam>> _teamStorage;
        private readonly PersistedState<AutobuilderWeightings> _autobuilderParams;

        public ProfileService(
            ILocalStorageService localStorageService,
            IApexChartService apexChartService,
            PokeApiService apiService,
            TypeChart typeChart
        )
        {
            _localStorageService = localStorageService;
            _apexChartService = apexChartService;
            _apiService = apiService;
            _typeChart = typeChart;

            _preferences = new(
                PREFERENCES_KEY,
                new() { DarkMode = true, AllowMultipleMegas = false, AllowMultipleGmax = false },
                Load<Preferences>,
                Save
            );
            _preferences.OnChanged += () => OnPreferencesChange?.Invoke();

            _allTypes = new(POKEMON_TYPES_KEY, [], Load<List<PokeApiNet.Type>>, Save);

            _pokemonStorage = new(POKEMON_STORAGE_KEY, new(), Load<PokemonStorage>, Save);
            _pokemonStorage.OnChanged += () => OnStorageChange?.Invoke();

            _teamStorage = new(TEAM_STORAGE_KEY, [], Load<List<PokemonTeam>>, Save);
            _teamStorage.OnChanged += () => OnTeamStorageChange?.Invoke();

            _autobuilderParams = new(
                AUTOBUILDER_PARAMS,
                new(
                    AutobuilderWeightings.MakeDefaultTypeWeightings(),
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
                ),
                Load<AutobuilderWeightings>,
                Save
            );
        }

        private async Task<T?> Load<T>(string key) => await _localStorageService.GetItemAsync<T>(key);

        private async Task Save<T>(string key, T value) => await _localStorageService.SetItemAsync(key, value);

        public bool IsDarkMode
        {
            get => _preferences.Value.DarkMode;
            set
            {
                _preferences.Set(_preferences.Value with { DarkMode = value });
                _ = SyncApexChartsThemeAsync(value);
            }
        }

        // ApexCharts renders via its own JS-side global options rather than the app's CSS theme, so it
        // needs to be told about dark mode explicitly - both when the user toggles it and when the
        // stored preference is first loaded, since that load bypasses the IsDarkMode setter above
        private Task SyncApexChartsThemeAsync(bool isDarkMode)
        {
            return _apexChartService.SetGlobalOptionsAsync(
                new ApexChartBaseOptions()
                {
                    Theme = new Theme
                    {
                        Palette = ApexCharts.PaletteType.Palette7,
                        Mode = isDarkMode ? ApexCharts.Mode.Dark : ApexCharts.Mode.Light,
                    },
                },
                true
            );
        }

        public bool AllowMultipleMegas
        {
            get => _preferences.Value.AllowMultipleMegas;
            set => _preferences.Set(_preferences.Value with { AllowMultipleMegas = value });
        }

        public bool AllowMultipleGmax
        {
            get => _preferences.Value.AllowMultipleGmax;
            set => _preferences.Set(_preferences.Value with { AllowMultipleGmax = value });
        }

        public List<PokeApiNet.Type> AllTypes
        {
            get => _allTypes.Value;
            set => _allTypes.Set(value);
        }

        public PokemonStorage PokemonStorage
        {
            get => _pokemonStorage.Value;
            set => _pokemonStorage.Set(value);
        }

        public List<PokemonTeam> TeamStorage
        {
            get => _teamStorage.Value;
            set => _teamStorage.Set(value);
        }

        public AutobuilderWeightings AutobuilderParams
        {
            get => _autobuilderParams.Value;
            set => _autobuilderParams.Set(value);
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
                    await _allTypes.LoadAsync();
                }

                // if profile doesn't contain pokemon types, generate them
                if (AllTypes.Count == 0)
                {
                    AllTypes = await _apiService.GetAllTypesAsync();
                }
                // the chart must be populated before the box/team deserialization below -
                // SmartPokemonJsonConverter resolves each pokemon's types against it mid-deserialize
                _typeChart.Populate(AllTypes);

                // load pokemon storage
                if (profileStorageVersion <= 1.3)
                {
                    PokemonBox? box = await _localStorageService.GetItemAsync<PokemonBox>(
                        POKEMON_STORAGE_KEY
                    );
                    if (box is not null)
                    {
                        _pokemonStorage.Value.Boxes.Add(box);
                    }
                    else if (_pokemonStorage.Value.Boxes.Count == 0)
                    {
                        _pokemonStorage.Value.Boxes.Add(new PokemonBox("Box 1"));
                    }

                    await _pokemonStorage.SetAsync(_pokemonStorage.Value);
                    await UpdateVersionAsync();
                }
                else
                {
                    await LoadPokemonStorageAsync();
                }

                // load team storage
                await _teamStorage.LoadAsync();

                // load autobuilder params
                await _autobuilderParams.LoadAsync();
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
            await _preferences.LoadAsync();
            await SyncApexChartsThemeAsync(_preferences.Value.DarkMode);

            return _preferences.Value;
        }

        public async Task LoadPokemonStorageAsync()
        {
            await _pokemonStorage.LoadAsync();
            if (_pokemonStorage.Value.Boxes.Count == 0)
            {
                _pokemonStorage.Value.Boxes.Add(new PokemonBox("Box 1"));
            }
        }

        public Task UpdatePokemonStorageAsync() => _pokemonStorage.SetAsync(PokemonStorage);

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

        public Task SetTeamStorageAsync(List<PokemonTeam> teamStorage) =>
            _teamStorage.SetAsync(teamStorage);

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

        public Task SetAutobuilderParamsAsync(AutobuilderWeightings weightings) =>
            _autobuilderParams.SetAsync(weightings);
    }
}
