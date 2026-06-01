using KurisuBot.Core.Infrastructure.Enums;

namespace KurisuBot.Core.Models
{
    public sealed class KCard
    {
        public long Id { get; }
        public KUser? Owner { get; private set; }
        public KVariant Variant { get; }
        public DateTime TimeObtained { get; private set; }
        public int? BorderId { get; private set; }
        public int? OverlayId { get; private set; }

        internal KCard(long id, KUser? owner, KVariant variant, DateTime timeObtained, int? borderId, int? overlayId)
        {
            Id = id;
            Owner = owner;
            Variant = variant;
            TimeObtained = timeObtained;
            BorderId = borderId;
            OverlayId = overlayId;
        }

        internal void SetOwner(KUser owner)
        {
            Owner = owner;
        }

        public override bool Equals(object? obj)
        {
            return obj is KCard other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"Card (ID: {Id}) [{Variant}]";
        }
    }
}
