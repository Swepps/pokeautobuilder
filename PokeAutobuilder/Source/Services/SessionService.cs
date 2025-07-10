using Accord;
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

        // the session team should always contain 6 members
        private PokemonTeam _pokemonTeam = new(); 
        public PokemonTeam Team 
        {
            get => _pokemonTeam;
            set
            {
                _ = SetTeamAsync(value);
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
            if (_pokemonTeam.Pokemon.Count == 0)
            {
                // initialize with an empty team of 6
                for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
                {
                    _pokemonTeam.Pokemon.Add(null);
                }
            }
            _searchLocation = taskSearchLocation.Result ?? "National Pokédex";
        }

        public async Task ClearSessionDataAsync()
        {
            await _sessionStorageService.ClearAsync();
        }

        public async Task SetTeamAsync(PokemonTeam team)
        {
            _pokemonTeam = team;
            // ensure the team has the correct number of Pokemon
            if (_pokemonTeam.Pokemon.Count < PokemonTeam.MaxTeamSize)
            {
                for (int i = _pokemonTeam.Pokemon.Count; i < PokemonTeam.MaxTeamSize; i++)
                {
                    _pokemonTeam.Pokemon.Add(null);
                }
            }
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

        public async Task SetSearchLocationAsync(string searchLocation)
        {
            _searchLocation = searchLocation;
            await _sessionStorageService.SetItemAsync(SEARCH_LOCATION, searchLocation);
        }
    }
}
