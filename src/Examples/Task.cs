using Luger.Functional;

using System.Threading.Tasks;

namespace Luger.Examples
{
    interface ILogService
    {
        // Task has no result.
        Task WriteAsync(string message);
    }

    interface IProcessor<TIn, TOut>
    {
        // Task<T> has a result of type T.
        Task<TOut> Process(TIn input);
    }

    class TaskExample<TIn, TOut>
    {
        private readonly IProcessor<TIn, TOut> processor;
        private readonly ILogService logService;

        public TaskExample(IProcessor<TIn, TOut> processor, ILogService logService)
        {
            this.processor = processor;
            this.logService = logService;
        }

        public async Task<TOut> ProcessAndLog(TIn input)
        {
            // Await processing to serialize logging.
            var result = await processor.Process(input);

            // Await logging
            await logService.WriteAsync("Processed something");

            // Return result.
            return result;
        }

        // LINQ below serialize invocation of processing and logging.
        // AsVoidTask wraps Task in Task<ValueTuple>.
        // ValueTuple is a 0-dimensional tuple and can only have one value: default. It's like a typed void.
        public Task<TOut> ProcessAndLog2(TIn input)
            => from result in processor.Process(input)
               from _ in logService.WriteAsync("Processed something").AsVoidTask()
               select result;
    }
}
