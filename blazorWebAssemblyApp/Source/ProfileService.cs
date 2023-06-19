using Blazored.LocalStorage;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace blazorWebAssemblyApp.Source
{
	public record Preferences
	{
		public bool DarkMode { get; init; }
	}

    public record UserData
    {
        public PokemonStorage Storage { get; init; } = new PokemonStorage();
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

		public async Task SetDarkMode(bool isDarkMode)
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


        public async Task UpdateUserDataAsync()
        {
            UserData data = new UserData
            {
                Storage = Globals.PokemonStorage
            };

            await SetUserDataAsync(data);
        }
    }
}
