namespace KurisuCore.Enums
{
    public enum KPackSize
    {
        Small,
        Medium,
        Large
    }

    public static class KPackSizeExtensions
    {
        public static int GetCardCount(this KPackSize size) => size switch
        {
            KPackSize.Small => 4,
            KPackSize.Medium => 7,
            KPackSize.Large => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };

        public static double GetPriceMultiplier(this KPackSize size) => size switch
        {
            KPackSize.Small => 1.0,
            KPackSize.Medium => 0.9,
            KPackSize.Large => 0.8,
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
    }
}
