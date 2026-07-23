namespace KurisuCore.Result
{
    public enum KResultSeverity
    {
        Success,
        Info,
        Warn,
        Fail
    }

    public static class KResultSeverityExtensions
    {
        private static readonly HashSet<KDetail> _info = new()
        {

        };

        private static readonly HashSet<KDetail> _warn = new()
        {

        };

        public static KResultSeverity ToSeverity(this KDetail detail) => detail switch
        {
            KDetail.Completed => KResultSeverity.Success,
            _ when _info.Contains(detail) => KResultSeverity.Info,
            _ when _warn.Contains(detail) => KResultSeverity.Warn,
            _ => KResultSeverity.Fail
        };
    }
}
