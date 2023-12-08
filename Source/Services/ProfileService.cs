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
        public List<SmartPokemonSerializable> OLD_Storage { get; init; } = new List<SmartPokemonSerializable>();
        public List<PokemonTeamSerializable> OLD_TeamStorage { get; init; } = new List<PokemonTeamSerializable>();
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

        public async Task CheckVersion()
        {
            double cachedVer = 0;
            if (await _localStorageService.ContainKeyAsync("version"))
                cachedVer = await _localStorageService.GetItemAsync<double>("version");

            if (cachedVer < Globals.Version)
            {
                bool dataFormatUpdate = false;
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
                //OLD_Storage = Globals.PokemonStorage.GetSerializableList(),
                Storage = Globals.PokemonStorage
			};

            await SetUserDataAsync(newdata);
        }

        public async Task LoadGlobalStorage()
        {
            UserData userData = await GetUserDataAsync();

            // creates a list of null pokemon with same size as the stored list
            //List<SmartPokemon?> pokemonList = new List<SmartPokemon?>(new SmartPokemon?[userData.OLD_Storage.Count]);

            //// get the pokemon from the API using the list of serialised pokemon in the storage
            //// may not load them in the order they were saved
            //var getStorageTasks = userData.Storage.Select(async (pokemon, index) =>
            //{
            //    SmartPokemon? sp = await pokemon.GetSmartPokemon();
            //    if (sp is not null)
            //        pokemonList[index] = sp;
            //});
            //await Task.WhenAll(getStorageTasks);

            // add any successful deserialisations into global storage
            foreach (SmartPokemon? pokemon in userData.Storage)
            {
                if (pokemon is null) continue;

                Globals.PokemonStorage.Add(pokemon);
            }
        }

        public async Task<List<PokemonTeam>> FetchTeamStorage()
        {
            UserData userData = await GetUserDataAsync();
            //List<PokemonTeamSerializable> serializedTeams = userData.OLD_TeamStorage;
            //List<PokemonTeam> teams = new List<PokemonTeam>();

            //foreach (PokemonTeamSerializable st in serializedTeams)
            //{
            //    teams.Add(await st.GetPokemonTeam());
            //}

            //return teams;
            return userData.TeamStorage;
        }

        public async Task AddTeam(PokemonTeam team)
        {
            UserData userData = await GetUserDataAsync();
            //List<PokemonTeamSerializable> teams = userData.OLD_TeamStorage;
            //teams.Add(new PokemonTeamSerializable(team));

            //UserData newData = userData
            //    with
            //{
            //    OLD_TeamStorage = teams
            //};

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
