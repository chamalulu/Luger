using System;
using System.Threading.Tasks;
using Void = System.ValueTuple;

namespace Luger.Functional
{
    public static class TaskExt
    {
        public static async Task<TR> Map<T, TR>(this Task<T> task, Func<T, TR> f)
            => f(await task);

        public static async Task<TR> Apply<T, TR>(this Task<Func<T, TR>> tf, Task<T> tt)
            => (await tf)(await tt);
        
        public static Task<Func<T2, TR>> Apply<T1, T2, TR>(this Task<Func<T1, T2, TR>> tf, Task<T1> tt)
            => Apply(tf.Map(FuncExt.Curry), tt);

        public static Task<Func<T2, T3, TR>> Apply<T1, T2, T3, TR>(this Task<Func<T1, T2, T3, TR>> tf, Task<T1> tt)
            => Apply(tf.Map(FuncExt.CurryFirst), tt); 

        public static async Task<TR> Bind<T, TR>(this Task<T> task, Func<T, Task<TR>> f)
            => await f(await task);

        #region LINQ query syntax implementation

        public static Task<TR> Select<T, TR>(this Task<T> task, Func<T, TR> f)
            => Map(task, f);

        public static Task<TR> SelectMany<T, TR>(this Task<T> task, Func<T, Task<TR>> f)
            => Bind(task, f);

        public static async Task<TR> SelectMany<T, TC, TR>(this Task<T> task, Func<T, Task<TC>> f, Func<T, TC, TR> p)
        {
            var t = await task;
            return p(t, await f(t));
        }

        #endregion

        public static async Task<T> OrElse<T>(this Task<T> task, Func<Task<T>, Task<T>> fallback)
        {
            try { return await task; }
            catch (OperationCanceledException) { throw; }
            catch { return await fallback(task); }
        }

        public static async Task<T> OrElse<T>(this Task<T> task, Func<Exception, Task<T>> fallback)
        {
            try { return await task; }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { return await fallback(ex); }
        }

        public static Task<T> OrElse<T>(this Task<T> task, Func<Task<T>> fallback)
            => task.OrElse((Task<T> _) => fallback());

        public static async Task<Void> AsVoidTask(this Task task)
        {
            await task;
            return default;
        }

        public static Task<T> ExponentialBackoff<T>(
            this Func<Task<T>> f,
            Random rng,
            uint retries = 8,
            uint delayMs = 100
        )
            => retries == 0
                ? f()
                : f().OrElse(() =>
                    from _ in Task.Delay((int)((rng.NextDouble() + 0.5) * delayMs)).AsVoidTask()
                    from t in ExponentialBackoff(f, rng, retries - 1, delayMs << 1)
                    select t
                );
    }
}