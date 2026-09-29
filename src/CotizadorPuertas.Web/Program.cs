using Blazored.LocalStorage;
using CotizadorPuertas.Web;
using CotizadorPuertas.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();

var url = builder.Configuration["Supabase:Url"] ?? "";
var key = builder.Configuration["Supabase:AnonKey"] ?? "";

builder.Services.AddScoped<LocalStorageSessionHandler>();
builder.Services.AddScoped(sp => new Supabase.Client(url, key, new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false,
    SessionHandler = sp.GetRequiredService<LocalStorageSessionHandler>(),
}));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<Armador>();
builder.Services.AddScoped<CotizacionService>();
builder.Services.AddScoped<DocumentoService>();

var host = builder.Build();

async Task Iniciar()
{
    try
    {
        await host.Services.GetRequiredService<Supabase.Client>().InitializeAsync();
        await host.Services.GetRequiredService<AuthService>().Inicializar();
    }
    catch
    {
        await host.Services.GetRequiredService<AuthService>().MarcarListo();
    }
}
_ = Iniciar();

await host.RunAsync();
