using Blazored.LocalStorage;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace pokeAutoBuilder.Source.Services
{
    public record Preferences
    {
        public bool DarkMode { get; init; }
    }

    public record UserData
    {
        public List<SmartPokemon> Storage { get; init; } = new List<SmartPokemon>();
        public List<PokemonTeam> TeamStorage { get; init; } = new List<PokemonTeam>();
    }

    public class ProfileService
    {
        private readonly ILocalStorageService _localStorageService;

        public ProfileService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
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
            bool prefersDarkMode = await Globals.MudThemeProvider!.GetSystemPreference();

            return new Preferences
            {
                DarkMode = prefersDarkMode
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

        // --- User data ---
        public async Task<UserData> GetUserDataAsync()
        {
            if (await _localStorageService.ContainKeyAsync("userdata"))
                return await _localStorageService.GetItemAsync<UserData>("userdata");

            return new UserData();
        }

        private async Task SetUserDataAsync(UserData data)
        {
            await _localStorageService.SetItemAsync("userdata", data);
        }


        public async Task UpdatePokemonStorageAsync()
        {
            UserData userData = await GetUserDataAsync();

            UserData newdata = userData
                with
            {
                Storage = Globals.PokemonStorage
			};

            await SetUserDataAsync(newdata);
        }

        public async Task LoadGlobalStorage()
        {
            UserData userData = await GetUserDataAsync();

            foreach (SmartPokemon? pokemon in userData.Storage)
            {
                if (pokemon is null) continue;

                Globals.PokemonStorage.Add(pokemon);
            }
        }

        public async Task<List<PokemonTeam>> FetchTeamStorage()
        {
            UserData userData = await GetUserDataAsync();
            return userData.TeamStorage;
        }

        public async Task AddTeam(PokemonTeam team)
        {
            UserData userData = await GetUserDataAsync();

            List<PokemonTeam> teams = userData.TeamStorage;
            teams.Add(team);

            UserData newData = userData
                with
            {
                TeamStorage = teams
            };

            await SetUserDataAsync(newData);
        }

        public async Task RemoveTeam(PokemonTeam team, int index = -1)
        {
            UserData userData = await GetUserDataAsync();
            List<PokemonTeam> teams = userData.TeamStorage;

            // try to find the first matching team
            if (index == -1)
            {
                index = teams.IndexOf(team);
            }

            if (index >= 0)
            {
                teams.RemoveAt(index);
            }

            UserData newData = userData
                with
            {
                TeamStorage = teams
            };

            await SetUserDataAsync(newData);
        }
    }
}
