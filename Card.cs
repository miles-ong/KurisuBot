namespace KurisuBot
{
    public enum CardRarity
    {
        R,
        SR,
        SSR,
        UR
    }

    public class Card
    {
        public long Id { get; private set; }
        public KurisuUser? Owner { get; set; }
        public KurisuCharacter? Character { get; set; }

        private void SetRandomId()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int randomBits = Random.Shared.Next(0, 9999);
            Id = long.Parse($"{timestamp}{randomBits:D4}");
        }
    }
}
