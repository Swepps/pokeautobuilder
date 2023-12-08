using Blazored.SessionStorage;

namespace pokeAutoBuilder.Source.Services
{
    public record SessionData
    {
        public PokemonTeam GlobalTeam { get; init; } = new ();
        public int PokemonSearchLocation { get; init; } = 0;
    }

    public class SessionService
    {
        private readonly ISessionStorageService _sessionStorageService;

        public SessionService(ISessionStorageService sessionStorageService)
        {
            _sessionStorageService = sessionStorageService;
        }

        public async Task<SessionData> GetSessionDataAsync()
        {
            if (await _sessionStorageService.ContainKeyAsync("session"))
                return await _sessionStorageService.GetItemAsync<SessionData>("session");


            return new SessionData();
        }

        private async Task SetSessionDataAsync(SessionData data)
        {
            await _sessionStorageService.SetItemAsync("session", data);
        }

        public async Task SetGlobalTeam(PokemonTeam team)
        {
            SessionData session = await GetSessionDataAsync();

            SessionData newSession = session
                with
            {
                GlobalTeam = team
            };

            await SetSessionDataAsync(newSession);
        }

        public async Task<PokemonTeam> FetchGlobalTeam()
        {
            SessionData sessionData = await GetSessionDataAsync();

            return sessionData.GlobalTeam;
        }

        public async Task SetPokemonSearchLocationAsync(int val)
        {
            SessionData session = await GetSessionDataAsync();
            SessionData newSession = session
                with
            {
                PokemonSearchLocation = val
            };
            await _sessionStorageService.SetItemAsync("session", newSession);
        }
    }
}
