using Blazored.LocalStorage;
using Blazored.SessionStorage;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using static MudBlazor.Colors;
using static pokeAutoBuilder.Pages.TeamBuilderPage;

namespace pokeAutoBuilder.Source.Services
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
        private static readonly string POKEMON_STORAGE_KEY = "pokemon_storage";
        private static readonly string TEAM_STORAGE_KEY = "pokemon_team_storage";

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
            var taskPokemonStorage = _localStorageService.GetItemAsync<PokemonStorage>(POKEMON_STORAGE_KEY);
            var taskTeamStorage = _localStorageService.GetItemAsync<List<PokemonTeam>>(TEAM_STORAGE_KEY);

            await Task.WhenAll(
                taskPokemonStorage.AsTask(),
                taskTeamStorage.AsTask()
                );

            _pokemonStorage = taskPokemonStorage.Result ?? new();
            _teamStorage = taskTeamStorage.Result ?? new();
        }

        public async Task<bool> CheckVersion()
        {
            bool dataFormatUpdate = false;
            double cachedVer = 0;
            if (await _localStorageService.ContainKeyAsync("version"))
                cachedVer = await _localStorageService.GetItemAsync<double>("version");

            if (cachedVer < Globals.Version)
            {
                
                foreach (KeyValuePair<double, bool> entry in Globals.Versions)
                {
                    if (entry.Key > cachedVer && entry.Value == true)
                    {
                        dataFormatUpdate = true;
                        break;
                    }
                }

                // clear the cache if any newer versions than the cached one require format update
                if (dataFormatUpdate)
                {
                    await _localStorageService.ClearAsync();
                }
            }

            // store the current version in the cache
            await _localStorageService.SetItemAsync("version", Globals.Version);

            return dataFormatUpdate;
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

        public async Task SetPokemonStorageAsync(PokemonStorage storage)
        {
            OnStorageChange?.Invoke();
            await _localStorageService.SetItemAsync(POKEMON_STORAGE_KEY, storage);
        }
        public async Task AddPokemonToStorageAsync(SmartPokemon pokemon)
        {
            PokemonStorage.Pokemon.Add(pokemon);
            await SetPokemonStorageAsync(PokemonStorage);
        }
        public async Task<bool> RemovePokemonFromStorageAsync(SmartPokemon pokemon)
        {
            bool removed = PokemonStorage.Pokemon.Remove(pokemon);
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
            TeamStorage.Add(team);
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
