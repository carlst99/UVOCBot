using System;

namespace UVOCBotRemora.Utilities
{
#nullable disable
    public sealed class Optional<T>
    {
        private readonly T _value;

        public bool HasValue { get; }

        public T Value => HasValue ? _value : throw new InvalidOperationException("Value is not set");

        private Optional(bool hasValue, T value)
        {
            HasValue = hasValue;
            _value = value;
        }

        public static Optional<T> FromValue(T value) => new(true, value);

        public static Optional<T> FromNoValue() => new(false, default);

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                T t => Equals(t),
                Optional<T> opt => Equals(opt),
                _ => false,
            };
        }

        public override int GetHashCode()
        {
            if (HasValue)
                return Value.GetHashCode();
            else
                return base.GetHashCode();
        }

        public bool Equals(T value)
        {
            return HasValue && ReferenceEquals(Value, value);
        }

        public bool Equals(Optional<T> opt)
        {
            if (!HasValue && !opt.HasValue)
                return true;
            else
                return HasValue == opt.HasValue && Value.Equals(opt.HasValue);
        }

        public override string ToString()
        {
            return $"Optional<{typeof(T)}> ({(HasValue ? Value.ToString() : "<no value>")})";
        }
    }
#nullable restore
}
