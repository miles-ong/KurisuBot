namespace KurisuCore.Enums
{
    public enum KBalance
    {
        Pity,
        QuizTokens,
        Coins,
        RDust,
        SRDust,
        SSRDust,
        URDust
    }

    public static class KBalanceExtensions
    {
        public static string ToDisplayString(this KBalance balance)
        {
            return balance switch
            {
                KBalance.Pity       => "Pity",
                KBalance.QuizTokens => "Quiz Tokens",
                KBalance.Coins      => "Coins",
                KBalance.RDust      => "R Dust",
                KBalance.SRDust     => "SR Dust",
                KBalance.SSRDust    => "SSR Dust",
                KBalance.URDust     => "UR Dust",
                _                   => "Unknown"
            };
        }
    }
}
