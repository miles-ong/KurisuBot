using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KurisuDesktop.Views;
using KurisuDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using KurisuDesktop.Factories;
using System;
using KurisuDesktop.Data;

namespace KurisuDesktop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var collection = new ServiceCollection();

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

            var services = collection.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainView
                {
                    DataContext = services.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}