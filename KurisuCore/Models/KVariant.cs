using KurisuCore.Enums;
using System.Text.Json.Serialization;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuCore.Models
{
    [method: JsonConstructor]
    public sealed class KVariant(
        KCharacter character,
        KAnime anime,
        KRarity rarity,
        string title,
        string? artist) 
        : KEntity<KVariantData>, IKLinkable<KPack>
    {
        public KCharacter Character { get; internal set; } = character;
        public KAnime Anime { get; internal set; } = anime;
        public KRarity Rarity { get; internal set; } = rarity;
        public string Title { get; internal set; } = title;
        public string? Artist { get; internal set; } = artist;
        public string ImageFileName => $"VARIANT_{Id}";
        public override string DisplayName => Title;

        private readonly HashSet<KPack> _packs = [];
        private readonly Lock _packsLock = new();

        [JsonIgnore]
        internal IEnumerable<KPack> Packs
        {
            get { lock (_packsLock) return _packs.ToList(); }
        }

        void IKLinkable<KPack>.Link(KPack pack)
        {
            lock (_packsLock) _packs.Add(pack);
        }

        void IKLinkable<KPack>.Unlink(KPack pack)
        {
            lock (_packsLock) _packs.Remove(pack);
        }
        
        internal bool ContainsPack(KPack pack)
        {
            lock (_packsLock) return _packs.Contains(pack);
        }

        internal override KVariantData ToData() => new(Id, Character.Id, Anime.Id, Rarity, Title, Artist);
        public override string ToString() => $"'{Character.DisplayName} {Title}' (ID: {Id})";
    }
}
