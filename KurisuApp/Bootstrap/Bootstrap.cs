using KurisuApp.Data;
using KurisuApp.Factories;
using KurisuApp.ViewModels;
using KurisuCore.APIClient;
using KurisuCore.BackblazeClient;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KurisuApp.Bootstrap
{
    public static class Bootstrapper
    {
        public static void RegisterCommonServices(IServiceCollection collection)
        {
            collection.AddSingleton(new KurisuAPIClient(Config.ApiUrl, Config.ApiKey));
            collection.AddSingleton<BackblazeClient>();

            collection.AddSingleton<MainViewModel>();
            collection.AddTransient<AnimePageViewModel>();
            collection.AddTransient<CharacterPageViewModel>();
            collection.AddTransient<VariantPageViewModel>();

            collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch
            {
                ApplicationPageNames.Anime => x.GetRequiredService<AnimePageViewModel>(),
                ApplicationPageNames.Character => x.GetRequiredService<CharacterPageViewModel>(),
                ApplicationPageNames.Variant => x.GetRequiredService<VariantPageViewModel>(),
                _ => throw new InvalidOperationException()
            });

            collection.AddSingleton<PageFactory>();
        }
    }
}
