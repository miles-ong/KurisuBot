using KurisuAPI.Middleware;
using KurisuAPI.Services;

AppContext.SetSwitch("System.Net.DisableIPv6", true);

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("[KurisuAPI] Reading environment variables...");

string supabaseConnection = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION")
    ?? throw new Exception("Missing SUPABASE_CONNECTION");

Console.WriteLine("[KurisuAPI] Successfully read environment variables");

builder.Services.AddSingleton(_ => new KAnimeService(supabaseConnection));
builder.Services.AddSingleton(_ => new KCharacterService(supabaseConnection));
builder.Services.AddSingleton(_ => new KCharacterAnimeLinkService(supabaseConnection));
builder.Services.AddSingleton(_ => new KVariantService(supabaseConnection));
builder.Services.AddSingleton(sp => new KCardService(supabaseConnection, sp.GetRequiredService<KVariantService>()));
builder.Services.AddSingleton(sp => new KUserService(supabaseConnection, sp.GetRequiredService<KCardService>()));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

Console.WriteLine("[KurisuAPI] Initializing services...");

//await app.Services.GetRequiredService<KAnimeService>().InitializeAsync();
//await app.Services.GetRequiredService<KCharacterService>().InitializeAsync();
//await app.Services.GetRequiredService<KCharacterAnimeLinkService>().InitializeAsync();
//await app.Services.GetRequiredService<KVariantService>().InitializeAsync();
//app.Services.GetRequiredService<KCardService>().Initialize();
//app.Services.GetRequiredService<KUserService>().Initialize();

Console.WriteLine("[KurisuAPI] Successfully initialized services");

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();