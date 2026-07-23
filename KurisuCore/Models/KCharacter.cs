using System.Text.Json.Serialization;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuCore.Models
{
    [method: JsonConstructor]
    public sealed class KCharacter(string name) : KEntity<KCharacterData>, IKLinkable<KAnime>
    {
        public string Name { get; internal set; } = name;
        public override string DisplayName => Name;

        private readonly HashSet<KAnime> _anime = [];
        private readonly HashSet<KVariant> _variants = [];
        private readonly Lock _animeLock = new();
        private readonly Lock _variantsLock = new();

        [JsonIgnore]
        internal IEnumerable<KAnime> Anime
        {
            get { lock (_animeLock) return _anime.ToList(); }
        }

        [JsonIgnore]
        internal IEnumerable<KVariant> Variants
        {
            get { lock (_variantsLock) return _variants.ToList(); }
        }

        void IKLinkable<KAnime>.Link(KAnime anime)
        {
            lock (_animeLock) _anime.Add(anime);
        }

        void IKLinkable<KAnime>.Unlink(KAnime anime)
        {
            lock (_animeLock) _anime.Remove(anime);
        }

        internal bool ContainsAnime(KAnime anime)
        {
            lock (_animeLock) return _anime.Contains(anime);
        }

        internal void AddVariant(KVariant variant)
        {
            lock (_variantsLock) _variants.Add(variant);
        }

        internal void RemoveVariant(KVariant variant)
        {
            lock (_variantsLock) _variants.Remove(variant);
        }

        internal bool ContainsVariant(KVariant variant)
        {
            lock (_variantsLock) return _variants.Contains(variant);
        }

        internal override KCharacterData ToData() => new(Id, Name);
        public override string ToString() => $"'{Name}' (ID: {Id})";
    }
}
