using KurisuCore.Enums;
using System.Text.Json.Serialization;

namespace KurisuCore.Models
{
    public sealed class KVariant : IKurisuDisplayItem
    {
        public string DisplayName => Title;
        public int Id { get; }
        public KCharacter Character { get; private set; }
        public KAnime Anime { get; private set; }
        public KRarity Rarity { get; private set; }
        public string Title { get; private set; }
        public string ImageFileName => $"VARIANT_{Id}";

        [JsonConstructor]
        public KVariant(int id, KCharacter character, KAnime anime, KRarity rarity, string title)
        {
            Id = id;
            Character = character;
            Anime = anime;
            Rarity = rarity;
            Title = title;
        }

        internal KResult SetRarity(KRarity rarity)
        {
            if (rarity == Rarity) return KResult.IdenticalVariantRarity;

            Rarity = rarity;

            return KResult.Completed;
        }

        internal KResult SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return KResult.InvalidVariantTitle;

            string trimmed = title.Trim();

            if (trimmed == Title) return KResult.IdenticalVariantTitle;

            Title = trimmed;

            return KResult.Completed;
        }

        public override string ToString() 
            => $"{Character} '{Title}'";
    }
}
