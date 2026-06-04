using KurisuCore.Enums;

namespace KurisuCore.Result
{
    public readonly struct Result<T>
    {
        public bool Success { get; }
        public T? _value { get; }
        public KResult? _error { get; }

        public T Value => Success ? _value! : throw new InvalidOperationException("Cannot access Value when Success is false");
        public KResult Error => !Success ? _error!.Value : throw new InvalidOperationException("Cannot access Error when Success is true");

        private Result(T value)
        {
            Success = true;
            _value = value;
            _error = null;
        }

        private Result(KResult error)
        {
            Success = false;
            _value = default;
            _error = error;
        }

        public static Result<T> Ok(T value) => new(value);
        public static Result<T> Fail(KResult error) => new(error);
    }
}
