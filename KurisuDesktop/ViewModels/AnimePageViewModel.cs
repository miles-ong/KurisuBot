using KurisuDesktop.Data;

namespace KurisuDesktop.ViewModels
{
    public partial class AnimePageViewModel : PageViewModel
    {
        public string Test { get; set; } = "Anime test";

        public AnimePageViewModel()
        {
            PageName = ApplicationPageNames.Anime;
        }
    }
}
