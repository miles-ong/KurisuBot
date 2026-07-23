namespace KurisuCore.Result
{
    public readonly struct KResult<T>
    {
        public KResultSeverity Severity { get; }
        public KDetail Detail { get; }
        public bool Success => Severity != KResultSeverity.Fail;
        public T Value => Severity != KResultSeverity.Fail
            ? _value!
            : throw new InvalidOperationException("Cannot access Value when result is a failure.");

        private readonly T? _value;

        public static KResult<T> Ok(T value) => new(value, KResultSeverity.Success, KDetail.Completed);
        public static KResult<T> Info(T value, KDetail detail) => new(value, KResultSeverity.Info, detail);
        public static KResult<T> Warn(T value, KDetail detail) => new(value, KResultSeverity.Warn, detail);
        public static KResult<T> Fail(KDetail error) => new(error);

        private KResult(T value, KResultSeverity severity, KDetail detail)
        {
            Severity = severity;
            Detail = detail;
            _value = value;
        }

        private KResult(KDetail error)
        {
            Severity = KResultSeverity.Fail;
            Detail = error;
            _value = default;
        }
    }
}
