using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Core.Models
{
    public sealed class KAnime
    {
        public int Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }

        private readonly HashSet<KCharacter> _characters = new();
        private readonly object _charactersLock = new();

        public IEnumerable<KCharacter> Characters
        {
            get
            {
                lock (_charactersLock) return _characters.ToList();
            }
        }

        internal KAnime(int id, string title, string? description)
        {
            Id = id;
            Title = title;
            Description = string.IsNullOrWhiteSpace(description)
                ? "No description."
                : description;
        }

        internal KResult SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return KResult.InvalidAnimeTitle;

            string trimmed = title.Trim();

            if (trimmed == Title) return KResult.IdenticalAnimeTitle;

            Title = trimmed;

            return KResult.Completed;
        }

        internal KResult SetDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return KResult.InvalidAnimeDescription;

            string trimmed = description.Trim();

            if (trimmed == Description) return KResult.IdenticalAnimeDescription;

            Description = trimmed;

            return KResult.Completed;
        }

        internal void LinkCharacter(KCharacter character)
        {
            lock (_charactersLock) _characters.Add(character);
        }

        internal void UnlinkCharacter(KCharacter character)
        {
            lock (_charactersLock) _characters.Remove(character);
        }

        internal bool ContainsCharacter(KCharacter character)
        {
            lock (_charactersLock) return _characters.Contains(character);
        }

        public override string ToString()
        {
            return $"'{Title}' (ID: {Id})";
        }
    }
}
