using Blazored.SessionStorage;
using static pokeAutoBuilder.Pages.TeamBuilderPage;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace pokeAutoBuilder.Source.Services
{ 
    public class SessionService
    {
        private readonly ISessionStorageService _sessionStorageService;

        public event Action OnTeamChange;

        // global variables
        private static readonly string POKEMON_TEAM_KEY = "pokemon_team";
        private static readonly string NATIONAL_DEX_KEY = "national_pokedex";
        private static readonly string SEARCH_LOCATION_KEY = "search_location";

        private PokemonTeam _pokemonTeam = new(); 
        public PokemonTeam Team 
        {
            get => _pokemonTeam;
            set
            {
                _pokemonTeam = value;
                _ = SetTeamAsync(value);
            }
        }

        private SmartPokedex _nationalDex = new();
        public SmartPokedex NationalDex
        {
            get => _nationalDex;
            set
            {
                _nationalDex = value;
                _ = SetNationalDexAsync(value);
            }
        }

        private SearchLocation _searchLoc = 0;
        public SearchLocation SearchLocation
        {
            get => _searchLoc;
            set
            {
                _searchLoc = value;
                _ = SetSearchLocationAsync(value);
            }
        }

        public SessionService(ISessionStorageService sessionStorageService)
        {
            _sessionStorageService = sessionStorageService;
        }

        public async Task LoadSessionStorage()
        {
            var taskPokemonTeam = _sessionStorageService.GetItemAsync<PokemonTeam>(POKEMON_TEAM_KEY);
            var taskNationDex = _sessionStorageService.GetItemAsync<SmartPokedex>(NATIONAL_DEX_KEY);
            var taskSearchLocation = _sessionStorageService.GetItemAsync<SearchLocation>(SEARCH_LOCATION_KEY);

            await Task.WhenAll(
                taskPokemonTeam.AsTask(),
                taskNationDex.AsTask(),
                taskSearchLocation.AsTask()
                );

            _pokemonTeam = taskPokemonTeam.Result ?? new();
            _nationalDex = taskNationDex.Result ?? new();
            _searchLoc = taskSearchLocation.Result;
        }

        public async Task ClearSessionDataAsync()
        {
            await _sessionStorageService.ClearAsync();
        }

        public async Task SetTeamAsync(PokemonTeam team)
        {
            OnTeamChange?.Invoke();
            await _sessionStorageService.SetItemAsync(POKEMON_TEAM_KEY, team);
        }
        public async Task SetTeamPokemonAsync(int index, SmartPokemon? pokemon)
        {
            if (index < 0 || index >= Team.Pokemon.Count)
                return;

            Team.Pokemon[index] = pokemon;
            await SetTeamAsync(Team);
        }

        public async Task SetNationalDexAsync(SmartPokedex nationalDex)
        {
            await _sessionStorageService.SetItemAsync(NATIONAL_DEX_KEY, nationalDex);
        }

        public async Task SetSearchLocationAsync(SearchLocation location)
        {
            await _sessionStorageService.SetItemAsync(SEARCH_LOCATION_KEY, location);
        }
    }
}
