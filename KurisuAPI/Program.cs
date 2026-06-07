using KurisuAPI.Bootstrap;
using KurisuAPI.Middleware;
using KurisuAPI.Services;

const string _consoleTag = "[KurisuAPI]";

AppContext.SetSwitch("System.Net.DisableIPv6", true);

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"{_consoleTag} Reading environment variables...");

string supabaseConnection = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION")
    ?? throw new Exception("Missing SUPABASE_CONNECTION");

Console.WriteLine($"{_consoleTag} Successfully read environment variables");

Console.WriteLine($"{_consoleTag} Registering services...");

Bootstrapper.RegisterServices(builder.Services, supabaseConnection);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

Console.WriteLine($"{_consoleTag} Successfully registered services");

var app = builder.Build();

Console.WriteLine($"{_consoleTag} Initializing services...");

app.Services.GetRequiredService<KCardService>().Initialize();
app.Services.GetRequiredService<KUserService>().Initialize();

Console.WriteLine($"{_consoleTag} Successfully initialized services");

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();