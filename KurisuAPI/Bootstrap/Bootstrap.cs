using KurisuAPI.Cache;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Services;

namespace KurisuAPI.Bootstrap
{
    public static class Bootstrapper
    {
        public static void RegisterServices(IServiceCollection collection, string connectionString)
        {
            collection.AddSingleton(new DbConnectionFactory(connectionString));

            collection.AddScoped<KAnimeRepository>();
            collection.AddScoped<KCharacterRepository>();
            collection.AddScoped<KVariantRepository>();
            collection.AddScoped<KCharacterAnimeLinkRepository>();
            collection.AddScoped<KUserRepository>();
            collection.AddScoped<KCardRepository>();

            collection.AddScoped<KAnimeService>();
            collection.AddScoped<KCharacterService>();
            collection.AddScoped<KVariantService>();
            collection.AddScoped<KCharacterAnimeLinkService>();
            collection.AddScoped<KCardService>();
            collection.AddScoped<KUserService>();

            collection.AddSingleton<KAnimeCache>();
            collection.AddSingleton<KCharacterCache>();
            collection.AddSingleton<KVariantCache>();
            collection.AddSingleton<KCardCache>();
            collection.AddSingleton<KUserCache>();

            collection.AddSingleton(sp => new Lazy<KCharacterAnimeLinkService>(sp.GetRequiredService<KCharacterAnimeLinkService>()));
        }
    }
}
