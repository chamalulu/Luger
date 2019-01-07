using System;
using System.Collections.Generic;
using System.Linq;
using Void = System.ValueTuple;

namespace Luger.Utilities
{
    public abstract class FuncMockBase<T, TR>
    {
        public readonly struct Call
        {
            public readonly DateTime Time;
            public readonly T Arguments;
            public readonly TR ReturnValue;

            public Call(DateTime time, T args, TR returnValue)
            {
                Time = time;
                Arguments = args;
                ReturnValue = returnValue;
            }
        }

        protected readonly Func<T, TR> Func;

        private readonly List<Call> _calls;

        public IEnumerable<Call> Calls => _calls;

        private Func<T, TR> Intercept(Func<T, TR> f)
            => args =>
            {
                var time = DateTime.Now;
                var rv = f(args);
                _calls.Add(new Call(time, args, rv));
                return rv;
            };

        protected FuncMockBase(Func<T, TR> @delegate)
        {
            Func = Intercept(@delegate);
            _calls = new List<Call>();
        }
    }

    public sealed class FuncMock<TR> : FuncMockBase<Void, TR>
    {
        public FuncMock(Func<TR> func)
            : base(_ => func()) { }
        
        public TR Invoke() => base.Func(default);
    }

    public sealed class FuncMock<T, TR> : FuncMockBase<T, TR>
    {
        public FuncMock(Func<T, TR> func)
            : base(func) { }
        
        public TR Invoke(T arg) => base.Func(arg);
    }

    public sealed class FuncMock<T1, T2, TR> : FuncMockBase<(T1, T2), TR>
    {
        public FuncMock(Func<T1, T2, TR> func)
            : base(args => func(args.Item1, args.Item2)) { }
        
        public TR Invoke(T1 arg1, T2 arg2) => base.Func((arg1, arg2));
    }

    public sealed class FuncMock<T1, T2, T3, TR> : FuncMockBase<(T1, T2, T3), TR>
    {
        public FuncMock(Func<T1, T2, T3, TR> func)
            : base(args => func(args.Item1, args.Item2, args.Item3)) { }
        
        public TR Invoke(T1 arg1, T2 arg2, T3 arg3) => base.Func((arg1, arg2, arg3));
    }
}