using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Core.Models
{
    public sealed class KCharacter
    {
        public int Id { get; }
        public string Name { get; private set; }
        private readonly HashSet<KAnime> _animes = new();
        private readonly HashSet<KVariant> _variants = new();
        private readonly object _animesLock = new();
        private readonly object _variantsLock = new();

        public IEnumerable<KAnime> Animes
        {
            get
            {
                lock (_animesLock) return _animes.ToList();
            }
        }

        public IEnumerable<KVariant> Variants
        {
            get
            {
                lock (_variantsLock) return _variants.ToList();
            }
        }

        internal KCharacter(int id, string name)
        {
            Id = id;
            Name = name;
        }

        internal KResult SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return KResult.InvalidCharacterName;

            string trimmed = name.Trim();

            if (trimmed == Name) return KResult.IdenticalCharacterName;

            Name = trimmed;

            return KResult.Completed;
        }

        internal void LinkAnime(KAnime anime)
        {
            lock (_animesLock) _animes.Add(anime);
        }

        internal void UnlinkAnime(KAnime anime)
        {
            lock (_animesLock) _animes.Remove(anime);
        }

        internal bool ContainsAnime(KAnime anime)
        {
            lock (_animesLock) return _animes.Contains(anime);
        }

        internal void AddVariant(KVariant variant)
        {
            lock (_variantsLock) _variants.Add(variant);
        }

        internal void RemoveVariant(KVariant variant)
        {
            lock (_variantsLock) _variants.Remove(variant);
        }

        public override string ToString()
        {
            return $"'{Name}' (ID: {Id})";
        }
    }
}
