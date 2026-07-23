using Dapper;
using KurisuAPI.Caches;
using KurisuAPI.Factories;
using KurisuAPI.Repositories;
using KurisuAPI.Repositories.Handlers;
using KurisuAPI.Services;

namespace KurisuAPI.Bootstrap
{
    public static class Bootstrapper
    {
        public static void RegisterServices(
            IServiceCollection collection,
            string supabaseConnection
            )
        {
            SqlMapper.AddTypeHandler(new KRarityTypeHandler());

            collection.AddSingleton(new DbConnectionFactory(supabaseConnection));

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

            collection.AddSingleton<KEntityFactory>();

            collection.AddSingleton<KAnimeService>();
            collection.AddSingleton<KCharacterService>();
            collection.AddSingleton<KVariantService>();
            collection.AddSingleton<KCharacterAnimeLinkService>();
            collection.AddSingleton<KCardService>();
            collection.AddSingleton<KUserService>();
        }
    }
}
