using Blazored.SessionStorage;
using PokemonDataModel;

namespace PokeAutobuilder.Source.Services
{
    public class SessionService
    {
        private readonly ISessionStorageService _sessionStorageService;
        private readonly TypeChart _typeChart;

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

        public SessionService(ISessionStorageService sessionStorageService, TypeChart typeChart)
        {
            _sessionStorageService = sessionStorageService;
            _typeChart = typeChart;

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

            EnsureTeamInitialized(Team);
        }

        // Warms every team member's multiplier cache for the team's own ruleset - a member
        // deserialized from storage only has its Unrestricted entry pre-warmed
        // (SmartPokemonJsonConverter can't know the team's ruleset), and a member freshly picked
        // from a search box may come from a different ruleset entirely. Downstream readers
        // (coverage/defense panes, TeamScorer) trust the cache is already populated. Called on
        // `team` directly (not the ambient Team property) so SetTeamAsync can warm an incoming
        // team - e.g. one loaded from Team Storage - before it becomes the active one.
        private void EnsureTeamInitialized(PokemonTeam team)
        {
            TypeChart chart = _typeChart.GetOrBuildDerived(team.Ruleset.Id, team.Ruleset.DisabledTypes);
            foreach (SmartPokemon? pokemon in team.Pokemon)
            {
                if (pokemon is not null && !pokemon.HasInitializedRuleset(team.Ruleset.Id))
                {
                    pokemon.InitializeTypes(chart, team.Ruleset);
                }
            }
        }

        public async Task ClearSessionDataAsync()
        {
            await _sessionStorageService.ClearAsync();
        }

        public Task UpdatePokemonTeamAsync() => SetTeamAsync(Team);

        // The one place a team becomes "the active session team" - callers (loading a saved team
        // into the editor, the auto-builder's best team, a slot edit below) don't need to remember
        // to warm it themselves; a team whose Pokemon haven't been touched since deserializing (e.g.
        // one just loaded from Team Storage) would otherwise throw the first time any coverage/
        // defense/score pane reads a multiplier for this team's ruleset.
        public Task SetTeamAsync(PokemonTeam team)
        {
            team = NormalizeTeamSize(team);
            EnsureTeamInitialized(team);
            return _team.SetAsync(team);
        }

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
