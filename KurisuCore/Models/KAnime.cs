using System.Text.Json.Serialization;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuCore.Models
{
    [method: JsonConstructor]
    public sealed class KAnime(
        string title,
        string? description) 
        : KEntity<KAnimeData>, IKLinkable<KCharacter>
    {
        public string Title { get; internal set; } = title;
        public string Description { get; internal set; } = description ?? "No description.";
        public override string DisplayName => Title;

        private readonly HashSet<KCharacter> _characters = [];
        private readonly Lock _charactersLock = new();

        [JsonIgnore]
        internal IEnumerable<KCharacter> Characters
        {
            get { lock (_charactersLock) return _characters.ToList(); }
        }

        void IKLinkable<KCharacter>.Link(KCharacter character)
        {
            lock (_charactersLock) _characters.Add(character);
        }

        void IKLinkable<KCharacter>.Unlink(KCharacter character)
        {
            lock (_charactersLock) _characters.Remove(character);
        }

        internal bool ContainsCharacter(KCharacter character)
        {
            lock (_charactersLock) return _characters.Contains(character);
        }

        internal override KAnimeData ToData() => new(Id, Title, Description);
        public override string ToString() => $"'{Title}' (ID: {Id})";
    }
}
