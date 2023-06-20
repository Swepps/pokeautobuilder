using Blazored.LocalStorage;
using Blazored.SessionStorage;

namespace blazorWebAssemblyApp.Source
{
    public record SessionData
    {
        public List<string> CurrentTeam { get; init; } = new List<string>
        {
            "",
            "",
            "",
            "",
            "",
            ""
        };

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

        public async Task SetTeamMemberAsync(int idx, string formName)
        {
            SessionData session = await GetSessionDataAsync();
            List<string> newTeam = session.CurrentTeam;

            if (idx >= 0 && idx < newTeam.Count)
            {
                newTeam[idx] = formName;
            }

            SessionData newSession = session
                with
            {
                CurrentTeam = newTeam
            };

            await _sessionStorageService.SetItemAsync("session", newSession);
        }

        public async Task LoadGlobalTeam()
        {
            SessionData sessionData = await GetSessionDataAsync();

            // check for any team already in the session data and use it to
            // load the global team
            var getTeamTasks = sessionData.CurrentTeam.Select(async (name, index) =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    SmartPokemon? sp = await PokeApiHandler.GetPokemonAsync(name);
                    if (sp is not null)
                        Globals.PokemonTeam[index] = sp;
                }
            });
            await Task.WhenAll(getTeamTasks);
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
