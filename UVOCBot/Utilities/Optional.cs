using System;

namespace UVOCBot.Utilities
{
    public sealed class Optional<T>
    {
        private readonly T? _value;

        public bool HasValue { get; }

        public T? Value => HasValue ? _value : throw new InvalidOperationException("Value is not set");

        private Optional(bool hasValue, T? value)
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
#nullable disable
            if (HasValue)
                return Value.GetHashCode();
            else
                return base.GetHashCode();
#nullable restore
        }

        public bool Equals(T value)
        {
            return HasValue && ReferenceEquals(Value, value);
        }

        public bool Equals(Optional<T> opt)
        {
#nullable disable
            if (!HasValue && !opt.HasValue)
                return true;
            else
                return HasValue == opt.HasValue && Value.Equals(opt.HasValue);
#nullable restore
        }

        public override string ToString()
        {
#nullable disable
            return $"Optional<{typeof(T)}> ({(HasValue ? Value.ToString() : "<no value>")})";
#nullable   restore
        }
    }
}
