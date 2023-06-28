using Blazored.LocalStorage;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace pokeAutoBuilder.Source
{
	public record Preferences
	{
		public bool DarkMode { get; init; }
	}

    public record UserData
    {
        public List<string> Storage { get; init; } = new List<string>();
        public List<PokemonTeamSerializable> TeamStorage { get; init; } = new List<PokemonTeamSerializable>();
    }

	public class ProfileService
	{
        private readonly ILocalStorageService _localStorageService;

        public ProfileService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
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
                Storage = Globals.PokemonStorage.GetAllNames()
            };

            await SetUserDataAsync(newdata);
        }

        public async Task LoadGlobalStorage()
        {
            UserData userData = await GetUserDataAsync();

            // get the pokemon from the API using the list of names in the storage
            // may not load them in the order they were saved
            var getStorageTasks = userData.Storage.Select(async name =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    SmartPokemon? sp = await PokeApiService.Instance!.GetPokemonAsync(name);
                    if (sp is not null)
                        Globals.PokemonStorage.Add(sp);
                }
            });
            await Task.WhenAll(getStorageTasks);
        }

        public async Task<List<PokemonTeam>> FetchTeamStorage()
        {
            UserData userData = await GetUserDataAsync();
            List<PokemonTeamSerializable> serializedTeams = userData.TeamStorage;
            List<PokemonTeam> teams = new List<PokemonTeam>();

            foreach (PokemonTeamSerializable st in serializedTeams)
            {
                teams.Add(await st.GetPokemonTeam());
            }

            return teams;   
        }

        public async Task AddTeam(PokemonTeam team)
        {
            UserData userData = await GetUserDataAsync();
            List<PokemonTeamSerializable> teams = userData.TeamStorage;
            teams.Add(new PokemonTeamSerializable(team));

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
			List<PokemonTeamSerializable> teams = userData.TeamStorage;
            PokemonTeamSerializable pts = new PokemonTeamSerializable(team);

            // try to find the first matching team
            if (index == -1)
            {
                index = teams.IndexOf(pts);
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
