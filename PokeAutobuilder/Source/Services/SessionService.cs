using Blazored.SessionStorage;
using PokemonDataModel;

namespace PokeAutobuilder.Source.Services
{
    public class SessionService
    {
        private readonly ISessionStorageService _sessionStorageService;

        public event Action? OnTeamChange;

        // global variables
        private static readonly string POKEMON_TEAM_KEY = "pokemon_team";
        private static readonly string SEARCH_LOCATION = "search_location";

        private readonly PersistedState<PokemonTeam> _team;
        private readonly PersistedState<string> _searchLocation;

        public PokemonTeam Team
        {
            get => _team.Value;
            set => _team.Set(NormalizeTeamSize(value));
        }

        public string SearchLocation
        {
            get => _searchLocation.Value;
            set => _searchLocation.Set(value);
        }

        public SessionService(ISessionStorageService sessionStorageService)
        {
            _sessionStorageService = sessionStorageService;

            _team = new(POKEMON_TEAM_KEY, new(), Load<PokemonTeam>, Save);
            _team.OnChanged += () => OnTeamChange?.Invoke();

            _searchLocation = new(SEARCH_LOCATION, "National Pokédex", Load<string>, Save);
        }

        private async Task<T?> Load<T>(string key) =>
            await _sessionStorageService.GetItemAsync<T>(key);

        private async Task Save<T>(string key, T value) =>
            await _sessionStorageService.SetItemAsync(key, value);

        public async Task LoadSessionStorage()
        {
            await Task.WhenAll(_team.LoadAsync(), _searchLocation.LoadAsync());

            if (_team.Value.Pokemon.Count == 0)
            {
                // initialize with an empty team of 6
                for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
                {
                    _team.Value.Pokemon.Add(null);
                }
            }
        }

        public async Task ClearSessionDataAsync()
        {
            await _sessionStorageService.ClearAsync();
        }

        public Task UpdatePokemonTeamAsync() => SetTeamAsync(Team);

        public Task SetTeamAsync(PokemonTeam team) => _team.SetAsync(NormalizeTeamSize(team));

        public async Task SetTeamPokemonAsync(int index, SmartPokemon? pokemon)
        {
            if (index < 0 || index >= Team.Pokemon.Count)
                return;

            Team.Pokemon[index] = pokemon;
            await SetTeamAsync(Team);
        }

        public Task SetSearchLocationAsync(string searchLocation) =>
            _searchLocation.SetAsync(searchLocation);

        private static PokemonTeam NormalizeTeamSize(PokemonTeam team)
        {
            // ensure the team has the correct number of Pokemon
            for (int i = team.Pokemon.Count; i < PokemonTeam.MaxTeamSize; i++)
            {
                team.Pokemon.Add(null);
            }
            return team;
        }
    }
}
