using System;

namespace Luger.Functional
{
    public struct Optional<T>
    {
        private readonly T _value;
        public readonly bool IsSome;

        private Optional(T value)
        {
            _value = value;
            IsSome = value != null;
        }

        public TR Match<TR>(Func<TR> none, Func<T, TR> some)
            => IsSome ? some(_value) : none();

        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        public static implicit operator Optional<T>(Optional.NonGenericNone _) => default;

        public static Optional<T> operator |(Optional<T> opt1, Optional<T> opt2)
            => opt1.IsSome ? opt1 : opt2;
        
        public static T operator |(Optional<T> opt, T t)
            => opt.IsSome ? opt._value : t;

        public string ToString(Func<T, string> formatter) => IsSome ? $"Some({formatter(_value)})" : "None"; 
        
        public override string ToString() => this.ToString(t => t.ToString());
    }

    public static class Optional
    {
        public class NonGenericNone {}

        public static NonGenericNone None => new NonGenericNone();

        public static Optional<T> Some<T>(T value) => value;
    }
}