using Accord.IO;
using AutoBuilder;
using Blazored.SessionStorage;
using PokeApiNet;
using PokemonDataModel;
using Utility;
using static MudBlazor.Colors;
using static PokeAutobuilder.Pages.TeamBuilderPage;

namespace PokeAutobuilder.Source.Services
{ 
    public class SessionService
    {
        private readonly ISessionStorageService _sessionStorageService;

        public event Action? OnTeamChange;

        // global variables
        private static readonly string POKEMON_TEAM_KEY = "pokemon_team";
        private static readonly string AUTOBUILDER_PARAMS = "autobuilder_params";
        private static readonly string SEARCH_LOCATION = "search_location";

        private PokemonTeam _pokemonTeam = new(); 
        public PokemonTeam Team 
        {
            get => _pokemonTeam;
            set
            {
                _ = SetTeamAsync(value);
            }
        }

        private AutoBuilderWeightings? _autobuilderParams = new();
        public AutoBuilderWeightings? AutobuilderParams
        {
            get => _autobuilderParams;
            set
            {
                if (value is not null)
                    _ = SetAutobuilderParamsAsync(value);
            }
        }

        private string _searchLocation = "National Pokédex";
        public string SearchLocation
        {
            get => _searchLocation;
            set
            {
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
            var taskAutobuilderParams = _sessionStorageService.GetItemAsync<AutoBuilderWeightings>(AUTOBUILDER_PARAMS);
            var taskSearchLocation = _sessionStorageService.GetItemAsync<string>(SEARCH_LOCATION);

            await Task.WhenAll(
                taskPokemonTeam.AsTask()
                , taskAutobuilderParams.AsTask()
                , taskSearchLocation.AsTask()
                );

            _pokemonTeam = taskPokemonTeam.Result ?? new();
            _autobuilderParams = taskAutobuilderParams.Result ?? null;
            _searchLocation = taskSearchLocation.Result ?? "National Pokédex";
        }

        public async Task ClearSessionDataAsync()
        {
            await _sessionStorageService.ClearAsync();
        }

        public async Task SetTeamAsync(PokemonTeam team)
        {
            _pokemonTeam = team;
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

        public async Task SetAutobuilderParamsAsync(AutoBuilderWeightings autobuilderParams)
        {
            _autobuilderParams = autobuilderParams;
            await _sessionStorageService.SetItemAsync(AUTOBUILDER_PARAMS, autobuilderParams);
        }

        public async Task SetSearchLocationAsync(string searchLocation)
        {
            _searchLocation = searchLocation;
            await _sessionStorageService.SetItemAsync(SEARCH_LOCATION, searchLocation);
        }
    }
}
