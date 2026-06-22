namespace KurisuCore.Models
{
    public sealed class KCardPack : IKurisuDisplayItem
    {
        public string DisplayName => string.Empty;
        public int Id { get; private set; }
    }
}
