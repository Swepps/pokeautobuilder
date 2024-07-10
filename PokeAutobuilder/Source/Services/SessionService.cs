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
        private static readonly string POKEMON_TYPES_KEY = "pokemon_types";
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
            // need to fetch all types before we can load any pokemon
            _allTypes = await _sessionStorageService.GetItemAsync<List<PokeApiNet.Type>>(POKEMON_TYPES_KEY);
            // if session doesn't contain pokemon types, generate them
            if (AllTypes is null || AllTypes.Count == 0)
            {
                AllTypes = await PokeApiService.Instance!.GetAllTypesAsync();
            }
            DataModelCache.LoadedTypes = AllTypes;

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

            // if session doesn't contain the national dex, generate it
            //if (NationalDex is null || NationalDex.Count == 0)
            //{
            //    NationalDex = (await PokeApiService.Instance!.GetNationalDexAsync())!;
            //}

            //// if the session doesn't contain the dexes for the version groups, generate those
            //if (VersionGroupDexes.Count == 0)
            //{
            //    NamedApiResourceList<VersionGroup> games = await PokeApiService.Instance!.GetVersionGroupsAsync();

            //    List<Task<VersionGroup>> versionGroupTasks = [];
            //    foreach (var game in games.Results)
            //    {
            //        versionGroupTasks.Add(PokeApiService.Instance.GetVersionGroupAsync(game));
            //    }
            //    await Task.WhenAll(versionGroupTasks);

            //    List<Task<Pokedex?>> pokedexTasks = [];
            //    foreach (var versionGroupTask in versionGroupTasks)
            //    {
            //        pokedexTasks.Clear();
            //        foreach (var pokedex in versionGroupTask.Result.Pokedexes)
            //        {
            //            pokedexTasks.Add(PokeApiService.Instance.GetPokedexAsync(pokedex));
            //        }
            //        await Task.WhenAll(pokedexTasks);

            //        SmartPokedex versionGroupDex = new(StringUtils.FirstCharToUpper(versionGroupTask.Result.Name));
            //        foreach (var pokedexTask in pokedexTasks)
            //        {
            //            if (pokedexTask.Result is not null)
            //                versionGroupDex.AddPokedex(pokedexTask.Result);
            //        }

            //        _versionGroupDexes.Add(versionGroupDex);
            //    }
            //}
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

        public async Task SetAllTypesAsync(List<PokeApiNet.Type> allTypes)
        {
			_allTypes = allTypes;
			await _sessionStorageService.SetItemAsync(POKEMON_TYPES_KEY, allTypes);
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
