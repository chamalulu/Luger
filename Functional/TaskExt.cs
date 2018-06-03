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


        public static async Task<T> OrElse<T>(this Task<T> task, Func<Task<T>> fallback)
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return await fallback();
            }
        }

        public static async Task<Void> AsVoidTask(this Task task)
        {
            await task;
            return default;
        }
    }
}