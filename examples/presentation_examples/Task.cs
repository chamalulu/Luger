using Luger.Functional;

using System.Threading.Tasks;

namespace Luger.Examples.presentation_examples
{
    interface IRepository
    {
        // Task has no result.
        Task StoreAsync(object? value);
    }

    interface IProcessor<TIn, TOut>
    {
        // Task<T> has a result of type T.
        Task<TOut> Process(TIn input);
    }

    class TaskExample<TIn, TOut>
    {
        readonly IProcessor<TIn, TOut> processor;
        readonly IRepository repository;

        public TaskExample(IProcessor<TIn, TOut> processor, IRepository repository)
        {
            this.processor = processor;
            this.repository = repository;
        }

        public async Task<TOut> ProcessAndLog(TIn input)
        {
            // Await processing to serialize logging.
            var result = await processor.Process(input);

            // Await logging
            await repository.StoreAsync(result);

            // Return result.
            return result;
        }

        // LINQ below serialize invocation of processing and logging.
        // AsVoidTask wraps Task in Task<ValueTuple>.
        // ValueTuple is a 0-dimensional tuple and can only have one value: default. It's like a typed void.
        public Task<TOut> ProcessAndLog2(TIn input)

            => from result in processor.Process(input)
               from _ in repository.StoreAsync(result).AsVoidTask()
               select result;
    }
}
