namespace KurisuCore.Enums
{
    public enum KColor
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        White,
        Gray,
        Black
    }

    public static class KColorExtensions
    {
        public static string ToHex(this KColor color)
        {
            return color switch
            {
                KColor.Red => "#EB3B3B",
                KColor.Orange => "#ED8D3B",
                KColor.Yellow => "#EBD33B",
                KColor.Green => "#46EB3B",
                KColor.Blue => "#3B81EB",
                KColor.Purple => "#843BEB",
                KColor.White => "#FFFFFF",
                KColor.Gray => "#BFBFBF",
                KColor.Black => "#000000",
                _ => "#000000"
            };
        }
    }
}
