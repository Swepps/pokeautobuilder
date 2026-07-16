using Blazored.LocalStorage;
using Blazored.LocalStorage.StorageOptions;
using Blazored.SessionStorage;
using Blazored.SessionStorage.StorageOptions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudBlazor;
using ApexCharts;
using PokeAutobuilder.Source.Services;
using PokeAutobuilder;
using PokemonDataModel;
using Blazor.Analytics;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddOidcAuthentication(options =>
{
    // Configure your authentication provider options here.
    // For more information, see https://aka.ms/blazor-standalone-auth
    builder.Configuration.Bind("Local", options.ProviderOptions);
});

builder.Services.AddMudServices(
    config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;

        config.SnackbarConfiguration.PreventDuplicates = true;
        config.SnackbarConfiguration.NewestOnTop = false;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 2000;
        config.SnackbarConfiguration.HideTransitionDuration = 500;
        config.SnackbarConfiguration.ShowTransitionDuration = 500;
        config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    });
builder.Services.AddApexCharts(e =>
{
    e.GlobalOptions = new ApexChartBaseOptions
    {
        Theme = new Theme { Palette = PaletteType.Palette6 },
    };
});
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();
// singleton (not scoped): the options-pattern Configure<TypeChart> below resolves it from the
// root provider, which DI scope validation forbids for scoped services. In Blazor WASM the two
// lifetimes are equivalent anyway (one scope per app instance).
builder.Services.AddSingleton<TypeChart>();
// deserialized SmartPokemon must have their types/multipliers resolved against the TypeChart -
// the converter does this inside the deserialization boundary for every persistence path
builder.Services
    .AddOptions<LocalStorageOptions>()
    .Configure<TypeChart>(
        (options, typeChart) =>
            options.JsonSerializerOptions.Converters.Add(new SmartPokemonJsonConverter(typeChart))
    );
builder.Services
    .AddOptions<SessionStorageOptions>()
    .Configure<TypeChart>(
        (options, typeChart) =>
            options.JsonSerializerOptions.Converters.Add(new SmartPokemonJsonConverter(typeChart))
    );
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddHttpClient<PokeApiService>();
builder.Services.AddGoogleAnalytics("G-SFB9MT9167");

await builder.Build().RunAsync();
