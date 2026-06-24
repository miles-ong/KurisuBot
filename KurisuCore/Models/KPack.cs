using KurisuCore.Enums;
using System.Text.Json.Serialization;

namespace KurisuCore.Models
{
    public sealed class KPack : IKurisuDisplayItem
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Price { get; private set; }
        public int SRWeight { get; private set; }
        public int SSRWeight { get; private set; }
        public int URWeight { get; private set; }
        public bool Enabled { get; private set; }

        public string DisplayName => Name;

        private readonly HashSet<KVariant> _variants = new();
        private readonly object _variantsLock = new();

        [JsonIgnore]
        internal IEnumerable<KVariant> Cards
        {
            get { lock (_variantsLock) return _variants.ToList(); }
        }

        [JsonConstructor]
        public KPack(
            int id, 
            string name, 
            int price, 
            int srWeight,
            int ssrWeight,
            int urWeight,
            bool enabled)
        {
            Id = id;
            Name = name;
            Price = price;
            SRWeight = srWeight;
            SSRWeight = ssrWeight;
            URWeight = urWeight;
            Enabled = enabled;
        }

        internal KResult SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return KResult.InvalidPackName;

            string trimmed = name.Trim();

            if (trimmed.Equals(Name)) return KResult.IdenticalPackName;

            Name = trimmed;

            return KResult.Completed;
        }

        internal KResult SetPrice(int price)
        {
            if (price < 0) return KResult.InvalidPackPrice;
            if (price == Price) return KResult.IdenticalPackPrice;

            Price = price;

            return KResult.Completed;
        }

        internal KResult SetWeights(int srWeight, int ssrWeight, int urWeight)
        {
            int srClamped = Math.Clamp(srWeight, 0, 100);
            int ssrClamped = Math.Clamp(ssrWeight, 0, 100);
            int urClamped = Math.Clamp(urWeight, 0, 100);
            int total = srClamped + ssrClamped + urClamped;

            if (total > 100) return KResult.InvalidPackWeights;

            SRWeight = srWeight;
            SSRWeight = ssrWeight;
            URWeight = urWeight;

            return KResult.Completed;
        }

        internal KResult SetEnabled(bool enabled)
        {
            if (enabled is true && Enabled is true) return KResult.PackIsAlreadyEnabled;
            if (enabled is false && Enabled is false) return KResult.PackIsAlreadyDisabled;

            Enabled = enabled;

            return KResult.Completed;
        }

        internal void LinkVariant(KVariant variant)
        {
            lock (_variantsLock) _variants.Add(variant);
        }

        internal void UnlinkVariant(KVariant variant)
        {
            lock (_variantsLock) _variants.Remove(variant);
        }

        internal bool ContainsVariant(KVariant variant)
        {
            lock (_variantsLock) return _variants.Contains(variant);
        }

        public override string ToString()
            => $"'{Name}' (ID: {Id})";

        public override bool Equals(object? obj)
            => obj is KPack other && other.Id == Id;

        public override int GetHashCode()
            => Id.GetHashCode();
    }
}