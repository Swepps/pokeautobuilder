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
    }

    public class SessionService
    {
        private readonly ISessionStorageService _sessionStorageService;

        public SessionService(ISessionStorageService sessionStorageService)
        {
            _sessionStorageService = sessionStorageService;
        }

        public async Task<SessionData> GetSessionData()
        {
            if (await _sessionStorageService.ContainKeyAsync("session"))
                return await _sessionStorageService.GetItemAsync<SessionData>("session");


            return new SessionData();
        }

        public async Task SetTeamMember(int idx, string formName)
        {
            SessionData session = await GetSessionData();
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
    }
}
