using System.Text.Json.Serialization;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuCore.Models
{
    [method: JsonConstructor]
    public sealed class KCard(
        KUser? owner,
        KVariant variant,
        DateTime timeObtained,
        int? borderId,
        int? overlayId,
        int xp)
        : KEntity<long, KCardData>
    {
        public KUser? Owner { get; internal set; } = owner;
        public KVariant Variant { get; internal set; } = variant;
        public DateTime TimeObtained { get; internal set; } = timeObtained;
        public int? BorderId { get; internal set; } = borderId;
        public int? OverlayId { get; internal set; } = overlayId;
        public int Xp { get; internal set; } = xp;
        public override string DisplayName => Variant.DisplayName;

        internal override KCardData ToData() => new(Id, Owner?.Id, Variant.Id, TimeObtained, BorderId, OverlayId, Xp);
        public override string ToString() => $"Card (ID: {Id}) [{Variant}]";
        public override bool Equals(object? obj) => obj is KCard other && other.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
