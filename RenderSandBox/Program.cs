using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace RenderSandBox
{
    internal record RenderJob(Rectangle Rect);

    internal record RenderProgress(RenderJob Job, float Percent);

    internal record RenderStarted(RenderJob Job) : RenderProgress(Job, 0f);

    internal record RenderFinished(RenderJob Job, Image<Rgb48> Result) : RenderProgress(Job, 1f);

    internal class Program
    {
        private static readonly IScene Scene = new TestScene(new Size(3440, 1440), 100);

        private static async Task Render(RenderJob job, IObserver<RenderProgress> observer, CancellationToken cancellationToken)
        {
            observer.OnNext(new RenderStarted(job));

            var (x, y, width, height) = job.Rect;

            // TODO: Render directly into image
            var buffer = new Rgb48[width * height];

            var progress = new Progress<float>(percent => observer.OnNext(new RenderProgress(job, percent)));

            await Scene.Render(job.Rect, buffer, progress, cancellationToken);

            var image = Image.LoadPixelData(buffer, width, height);

            observer.OnNext(new RenderFinished(job, image));

            observer.OnCompleted();
        }

        private static void CopyResultToBuffer(Image<Rgb48> image, RenderJob job, Image<Rgb48> result)
        {
            var rect = job.Rect;

            for (var row = 0; row < result.Height; row++)
            {
                var resultRowSpan = result.GetPixelRowSpan(row);
                var imageRowSpan = image.GetPixelRowSpan(rect.Y + row);
                resultRowSpan.CopyTo(imageRowSpan[rect.X..]);
            }
        }

        private static async Task<int> Main(string[] args)
        {
            var width = Scene.View.Width;
            var height = Scene.View.Height;

            Console.Error.WriteLine($"Rendering image {width}x{height}xRgb48... ");

            int xParts = 43, yParts = 18;

            var jobRectSize = new Size(Scene.View.Width / xParts, Scene.View.Height / yParts);

            var jobs = Enumerable.Range(0, yParts)
                .SelectMany(
                    _ => Enumerable.Range(0, xParts),
                    (y, x) => new RenderJob(new Rectangle(new Point(x * jobRectSize.Width, y * jobRectSize.Height), jobRectSize)))
                .ToList();

            using var memOwner = MemoryPool<Rgb48>.Shared.Rent(width * height);

            var image = Image.WrapMemory(memOwner, width, height);

            IObservable<RenderProgress> render(RenderJob job)
                => Observable.Create<RenderProgress>((observer, cancellationToken)
                    => Render(job, observer, cancellationToken));

            var connectableRendering = jobs
                .Select(render)
                .Merge(Environment.ProcessorCount, TaskPoolScheduler.Default)
                .Publish();

            var progressPercent = connectableRendering
                .Scan(
                    seed: (dict: ImmutableDictionary.Create<RenderJob, float>(), total: 0f),
                    accumulator: (acc, rp) => (acc.dict.SetItem(rp.Job, rp.Percent), acc.total - acc.dict.GetValueOrDefault(rp.Job) + rp.Percent))
                .Select(acc => acc.total / jobs.Count)
                .Publish()
                .RefCount();

            var handleRenderStarted = connectableRendering
                .OfType<RenderStarted>()
                .WithLatestFrom(progressPercent)
                .ForEachAsync(pair =>
                {
                    var ((job, _), percent) = pair;
                    Console.Error.WriteLine($"[{percent:P}] Rendering started for {job}.");
                });

            var handleRenderFinished = connectableRendering
                .OfType<RenderFinished>()
                .WithLatestFrom(progressPercent)
                .ForEachAsync(pair =>
                {
                    var ((job, result), percent) = pair;
                    CopyResultToBuffer(image, job, result);
                    Console.Error.WriteLine($"[{percent:P}] Rendering finished for {job}.");
                });

            using (connectableRendering.Connect())
            {
                await Task.WhenAll(handleRenderStarted, handleRenderFinished);
            }

            Console.Error.WriteLine("All rendering finished.");

            var encoder = new PngEncoder
            {
                BitDepth = PngBitDepth.Bit16,
                ColorType = PngColorType.Rgb,
                CompressionLevel = PngCompressionLevel.BestCompression,
            };

            var fileName = "render.png";

            var fileInfo = new FileInfo(fileName);

            Console.Error.Write($"Saving image to {fileInfo.FullName} ... ");

            image.Save("render.png", encoder);

            Console.Error.WriteLine("Done.");

            return 0;
        }
    }
}
