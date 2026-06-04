using KurisuDesktop.Data;

namespace KurisuDesktop.ViewModels
{
    public partial class CharacterPageViewModel : PageViewModel
    {
        public string Test { get; set; } = "Character test";

        public CharacterPageViewModel()
        {
            PageName = ApplicationPageNames.Character;
        }
    }
}
