using Accord.IO;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.VisualBasic;
using PokemonDataModel;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Utility;
using static MudBlazor.Colors;
using static PokeAutobuilder.Pages.TeamBuilderPage;

namespace PokeAutobuilder.Source.Services
{
    public record Preferences
    {
        public bool DarkMode { get; init; }
    }

    public class ProfileService
    {
        private readonly ILocalStorageService _localStorageService;

        public event Action? OnStorageChange;
        public event Action? OnTeamStorageChange;

        // global variables
        private static readonly string POKEMON_TYPES_KEY = "pokemon_types";
        private static readonly string POKEMON_STORAGE_KEY = "pokemon_storage";
        private static readonly string TEAM_STORAGE_KEY = "pokemon_team_storage";

        private List<PokeApiNet.Type> _allTypes = new();
        public List<PokeApiNet.Type> AllTypes
        {
            get => _allTypes;
            set
            {
                _ = SetAllTypesAsync(value);
            }
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

        private List<PokemonTeam> _teamStorage = new();
        public List<PokemonTeam> TeamStorage
        {
            get => _teamStorage;
            set
            {
                _teamStorage = value;
                _ = SetTeamStorageAsync(value);
            }
        }

        public ProfileService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
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
                    _allTypes = await _localStorageService.GetItemAsync<List<PokeApiNet.Type>>(POKEMON_TYPES_KEY);
                }

                // if profile doesn't contain pokemon types, generate them
                if (AllTypes is null || AllTypes.Count == 0)
                {
                    AllTypes = await PokeApiService.Instance!.GetAllTypesAsync();
                }
                DataModelCache.LoadedTypes = AllTypes;

                // load pokemon storage
                if (profileStorageVersion <= 1.3)
                {
                    PokemonBox box = await _localStorageService.GetItemAsync<PokemonBox>(POKEMON_STORAGE_KEY);
                    _pokemonStorage.Boxes.Add(box);
                    // replace old storage format with new format
                    await SetPokemonStorageAsync(_pokemonStorage);
                    await UpdateVersionAsync();
                }
                else
                {
                    _pokemonStorage = await _localStorageService.GetItemAsync<PokemonStorage>(POKEMON_STORAGE_KEY);
                    if (_pokemonStorage is null)
                    {
                        _pokemonStorage = new();
                        _pokemonStorage.Boxes.Add(new PokemonBox("Pokémon Storage"));
                    }
                }

                // load team storage
                _teamStorage = await _localStorageService.GetItemAsync<List<PokemonTeam>>(TEAM_STORAGE_KEY);
                _teamStorage ??= new();
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
            if (await _localStorageService.ContainKeyAsync("version"))
                return await _localStorageService.GetItemAsync<double>("version");

            return Globals.Version;
        }

        public async Task UpdateVersionAsync()
        {
            // store the current version in the cache
            await _localStorageService.SetItemAsync("version", Globals.Version);
        }

        // --- Preferences ---
        public async Task<Preferences> GetPreferencesAsync()
        {
            // if they've already specified their preferences explicitly, use them
            if (await _localStorageService.ContainKeyAsync("preferences"))
                return await _localStorageService.GetItemAsync<Preferences>("preferences");

            // else default to OS settings...
            // TODO, get theme provider
            //bool prefersDarkMode = await Globals.MudThemeProvider!.GetSystemPreference();

            return new Preferences
            {
                DarkMode = true
            };
        }

        public async Task SetDarkModeAsync(bool isDarkMode)
        {
            Preferences prefs = await GetPreferencesAsync();
            Preferences newPrefs = prefs
                with
            { DarkMode = isDarkMode };

            await _localStorageService.SetItemAsync("preferences", newPrefs);
        }

        public async Task SetAllTypesAsync(List<PokeApiNet.Type> allTypes)
        {
            _allTypes = allTypes;
            await _localStorageService.SetItemAsync(POKEMON_TYPES_KEY, allTypes);
        }

        public async Task SetPokemonStorageAsync(PokemonStorage storage)
        {
            OnStorageChange?.Invoke();
            await _localStorageService.SetItemAsync(POKEMON_STORAGE_KEY, storage);
        }
        public async Task AddPokemonToStorageAsync(SmartPokemon pokemon)
        {
            PokemonStorage.Boxes[0].Pokemon.Add(pokemon);
            await SetPokemonStorageAsync(PokemonStorage);
        }
        public async Task<bool> RemovePokemonFromStorageAsync(SmartPokemon pokemon)
        {
            bool removed = PokemonStorage.Boxes[0].Pokemon.Remove(pokemon);
            if (removed)
                await SetPokemonStorageAsync(PokemonStorage);

            return removed;
        }

        public async Task SetTeamStorageAsync(List<PokemonTeam> teamStorage)
        {
            OnTeamStorageChange?.Invoke();
            await _localStorageService.SetItemAsync(TEAM_STORAGE_KEY, teamStorage);
        }
        public async Task AddTeamToStorageAsync(PokemonTeam team)
        {
            TeamStorage.Add(new PokemonTeam(team));
            await SetTeamStorageAsync(TeamStorage);
        }
        public async Task<bool> RemoveTeamFromStorageAsync(PokemonTeam team)
        {
            bool removed = TeamStorage.Remove(team);
            if (removed)
                await SetTeamStorageAsync(TeamStorage);

            return removed;
        }
    }
}
