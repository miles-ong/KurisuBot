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

            collection.AddSingleton<KAnimeRepository>();
            collection.AddSingleton<KCharacterRepository>();
            collection.AddSingleton<KVariantRepository>();
            collection.AddSingleton<KCharacterAnimeLinkRepository>();
            collection.AddSingleton<KUserRepository>();
            collection.AddSingleton<KCardRepository>();

            collection.AddSingleton<KAnimeCache>();
            collection.AddSingleton<KCharacterCache>();
            collection.AddSingleton<KVariantCache>();
            collection.AddSingleton<KCardCache>();
            collection.AddSingleton<KUserCache>();

            collection.AddSingleton<KAnimeService>();
            collection.AddSingleton<KCharacterService>();
            collection.AddSingleton<KVariantService>();
            collection.AddSingleton<KCharacterAnimeLinkService>();
            collection.AddSingleton(sp => new Lazy<KCharacterAnimeLinkService>(sp.GetRequiredService<KCharacterAnimeLinkService>));
            collection.AddSingleton<KCardService>();
            collection.AddSingleton<KUserService>();
        }
    }
}
