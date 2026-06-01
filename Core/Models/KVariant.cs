using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Core.Models
{
    public sealed class KVariant
    {
        public int Id { get; }
        public KCharacter Character { get; private set; }
        public KAnime Anime { get; private set; }
        public KRarity Rarity { get; private set; }
        public string Title { get; private set; }

        internal KVariant(int id, KCharacter character, KAnime anime, KRarity rarity, string title)
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

            if(trimmed == Title) return KResult.IdenticalVariantTitle;

            Title = trimmed;

            return KResult.Completed;
        }

        public override string ToString()
        {
            return $"{Character} '{Title}'";
        }
    }
}
