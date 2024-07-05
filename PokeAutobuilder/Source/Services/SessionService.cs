using Accord.IO;
using AutoBuilder;
using Blazored.SessionStorage;
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
        private static readonly string NATIONAL_DEX_KEY = "national_pokedex";
        private static readonly string POKEMON_TYPES_KEY = "pokemon_types";
        private static readonly string AUTOBUILDER_PARAMS = "autobuilder_params";

        private PokemonTeam _pokemonTeam = new(); 
        public PokemonTeam Team 
        {
            get => _pokemonTeam;
            set
            {
                _ = SetTeamAsync(value);
            }
        }

        private SmartPokedex _nationalDex = new();
        public SmartPokedex NationalDex
        {
            get => _nationalDex;
            set
            {                
                _ = SetNationalDexAsync(value);
            }
        }

        private List<PokeApiNet.Type> _allTypes = new();
        public List<PokeApiNet.Type> AllTypes
        {
            get => _allTypes;
            set
            {                
                _ = SetAllTypesAsync(value);
            }
        }

        private AutoBuilderWeightings? _autobuilderParams = new();
        public AutoBuilderWeightings? AutobuilderParams
        {
            get => _autobuilderParams;
            set
            {
                if (value is not null)
                    _ = SetAutobuilderParams(value);
            }
        }

        public SessionService(ISessionStorageService sessionStorageService)
        {
            _sessionStorageService = sessionStorageService;
        }

        public async Task LoadSessionStorage()
        {
            // need to fetch all types before we can load any pokemon
            _allTypes = await _sessionStorageService.GetItemAsync<List<PokeApiNet.Type>>(POKEMON_TYPES_KEY);
            // if session doesn't contain pokemon types, generate them
            if (AllTypes is null || AllTypes.Count == 0)
            {
                AllTypes = await PokeApiService.Instance!.GetAllTypesAsync();
            }
            DataModelCache.LoadedTypes = AllTypes;

            var taskPokemonTeam = _sessionStorageService.GetItemAsync<PokemonTeam>(POKEMON_TEAM_KEY);
            var taskNationDex = _sessionStorageService.GetItemAsync<SmartPokedex>(NATIONAL_DEX_KEY);
            var taskAutobuilderParams = _sessionStorageService.GetItemAsync<AutoBuilderWeightings>(AUTOBUILDER_PARAMS);

            await Task.WhenAll(
                taskPokemonTeam.AsTask()
                , taskNationDex.AsTask()
                , taskAutobuilderParams.AsTask()
                );

            _pokemonTeam = taskPokemonTeam.Result ?? new();
            _nationalDex = taskNationDex.Result ?? new();
            _autobuilderParams = taskAutobuilderParams.Result ?? null;

            // if session doesn't contain the national dex, generate it
            if (NationalDex is null || NationalDex.Count == 0)
            {
                NationalDex = (await PokeApiService.Instance!.GetNationalDexAsync())!;
            }
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

        public async Task SetNationalDexAsync(SmartPokedex nationalDex)
        {
			_nationalDex = nationalDex;
			await _sessionStorageService.SetItemAsync(NATIONAL_DEX_KEY, nationalDex);
        }

        public async Task SetAllTypesAsync(List<PokeApiNet.Type> allTypes)
        {
			_allTypes = allTypes;
			await _sessionStorageService.SetItemAsync(POKEMON_TYPES_KEY, allTypes);
        }

        public async Task SetAutobuilderParams(AutoBuilderWeightings autobuilderParams)
        {
            _autobuilderParams = autobuilderParams;
            await _sessionStorageService.SetItemAsync(AUTOBUILDER_PARAMS, autobuilderParams);
        }
    }
}
