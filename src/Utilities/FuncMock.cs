using System;
using System.Collections.Generic;
using Void = System.ValueTuple;

namespace Luger.Utilities
{
    public readonly struct Invocation<T, TR>
    {
        public DateTime Time { get; }
        public T Arguments { get; }
        public TR ReturnValue { get; }

        public Invocation(DateTime time, T args, TR returnValue)
        {
            Time = time;
            Arguments = args;
            ReturnValue = returnValue;
        }
    }

    public abstract class FuncMockBase<T, TR>
    {
        private readonly List<Invocation<T, TR>> _calls;

        protected Func<T, TR> Func { get; }

        public IEnumerable<Invocation<T, TR>> Calls => _calls.AsReadOnly();

        private Func<T, TR> Intercept(Func<T, TR> f)
            => args =>
            {
                var time = DateTime.Now;
                var rv = f(args);
                _calls.Add(new Invocation<T, TR>(time, args, rv));
                return rv;
            };

        protected FuncMockBase(Func<T, TR> @delegate)
        {
            Func = Intercept(@delegate);
            _calls = new List<Invocation<T, TR>>();
        }
    }

    public sealed class FuncMock<TR> : FuncMockBase<Void, TR>
    {
        public FuncMock(Func<TR> func)
            : base(_ => func()) { }
        
        public TR Invoke() => Func(default);
    }

    public sealed class FuncMock<T, TR> : FuncMockBase<T, TR>
    {
        public FuncMock(Func<T, TR> func)
            : base(func) { }
        
        public TR Invoke(T arg) => Func(arg);
    }

    public sealed class FuncMock<T1, T2, TR> : FuncMockBase<(T1, T2), TR>
    {
        public FuncMock(Func<T1, T2, TR> func)
            : base(args => func(args.Item1, args.Item2)) { }
        
        public TR Invoke(T1 arg1, T2 arg2) => Func((arg1, arg2));
    }

    public sealed class FuncMock<T1, T2, T3, TR> : FuncMockBase<(T1, T2, T3), TR>
    {
        public FuncMock(Func<T1, T2, T3, TR> func)
            : base(args => func(args.Item1, args.Item2, args.Item3)) { }
        
        public TR Invoke(T1 arg1, T2 arg2, T3 arg3) => Func((arg1, arg2, arg3));
    }
}
