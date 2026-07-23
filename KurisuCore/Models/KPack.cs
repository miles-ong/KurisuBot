using KurisuCore.Enums;
using System.Text.Json.Serialization;
using KurisuCore.Models.Bases;
using KurisuCore.Models.Data;

namespace KurisuCore.Models
{
    [method: JsonConstructor]
    public sealed class KPack(
        bool enabled,
        string name,
        KPackType packType,
        int price,
        int discount,
        int fixedCardCount,
        int srWeight,
        int ssrWeight,
        int urWeight) 
        : KEntity<KPackData>, IKLinkable<KVariant>
    {
        public bool Enabled { get; internal set; } = enabled;
        public string Name { get; internal set; } = name;
        public KPackType PackType { get; internal set; } = packType;
        public int Price { get; internal set; } = price;
        public int Discount { get; internal set; } = discount;
        public int FixedCardCount { get; internal set; } = fixedCardCount;
        public int SrWeight { get; internal set; } = srWeight;
        public int SsrWeight { get; internal set; } = ssrWeight;
        public int UrWeight { get; internal set; } = urWeight;
        public override string DisplayName => Name;

        private readonly HashSet<KVariant> _variants = [];
        private readonly Lock _variantsLock = new();

        [JsonIgnore]
        internal IEnumerable<KVariant> Variants
        {
            get { lock (_variantsLock) return _variants.ToList(); }
        }

        void IKLinkable<KVariant>.Link(KVariant variant)
        {
            lock (_variantsLock) _variants.Add(variant);
        }

        void IKLinkable<KVariant>.Unlink(KVariant variant)
        {
            lock (_variantsLock) _variants.Remove(variant);
        }

        internal bool ContainsVariant(KVariant variant)
        {
            lock (_variantsLock) return _variants.Contains(variant);
        }

        internal override KPackData ToData() => new(Id, Enabled, Name, PackType, Price, Discount, FixedCardCount, SrWeight, SsrWeight, UrWeight);
        public override string ToString() => $"'{Name}' (ID: {Id})";
        public override bool Equals(object? obj) => obj is KPack other && other.Id == Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}