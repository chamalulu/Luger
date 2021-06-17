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
    internal record RenderJob
    {
        public RenderJob(Image<Rgb48> target, Rectangle rect)
        {
            if (!target.Bounds().Contains(rect))
            {
                throw new ArgumentOutOfRangeException(nameof(rect));
            }

            Target = target;
            Rect = rect;
        }

        public Image<Rgb48> Target { get; private init; }

        public Rectangle Rect { get; private init; }

        /// <summary>
        /// Gets the representation of the pixels in <see cref="Target"/> as a <see cref="Span{T}"/> of contiguous memory at row
        /// <paramref name="rowIndex"/> offset and bounded by <see cref="Rect"/>.
        /// </summary>
        /// <param name="rowIndex">Index of row relative to <see cref="Rect"/>.</param>
        public Span<Rgb48> GetPixelRowSpan(int rowIndex)

            => rowIndex >= 0 && rowIndex < Rect.Height
                ? Target.GetPixelRowSpan(Rect.Y + rowIndex).Slice(Rect.X, Rect.Width)
                : throw new ArgumentOutOfRangeException(nameof(rowIndex));
    }

    internal record RenderProgress(RenderJob Job, float Percent);

    internal record RenderStarted(RenderJob Job) : RenderProgress(Job, 0f);

    internal record RenderFinished(RenderJob Job) : RenderProgress(Job, 1f);

    internal class Program
    {
        private static readonly IScene Scene = new TestScene(new Size(3440, 1440), 100);

        private static async Task Render(RenderJob job, IObserver<RenderProgress> observer, CancellationToken cancellationToken)
        {
            observer.OnNext(new RenderStarted(job));

            var progress = new Progress<float>(percent => observer.OnNext(new RenderProgress(job, percent)));

            await Scene.Render<Rgb48>(job, progress, cancellationToken);

            observer.OnNext(new RenderFinished(job));

            observer.OnCompleted();
        }

        private static async Task<int> Main(string[] args)
        {
            var width = Scene.View.Width;
            var height = Scene.View.Height;

            Console.Error.WriteLine($"Rendering image {width}x{height}xRgb48... ");

            int xParts = 43, yParts = 18;

            var jobRectSize = new Size(Scene.View.Width / xParts, Scene.View.Height / yParts);

            using var memOwner = MemoryPool<Rgb48>.Shared.Rent(width * height);

            var image = Image.WrapMemory(memOwner, width, height);

            var jobs = Enumerable.Range(0, yParts)
                .SelectMany(
                    _ => Enumerable.Range(0, xParts),
                    (y, x) => new RenderJob(
                        image,
                        new Rectangle(new Point(x * jobRectSize.Width, y * jobRectSize.Height), jobRectSize)))
                .ToList();

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
                    var ((job, _), percent) = pair;
                    Console.Error.WriteLine($"[{percent:P}] Rendering finished for {job}.");
                });

            using (connectableRendering.Connect())
            {
                await Task.WhenAll(handleRenderStarted, handleRenderFinished);
            }

            Console.Error.WriteLine("All rendering finished.");

            var fileInfo = new FileInfo("render.png");

            Console.Error.Write($"Saving image to {fileInfo.FullName} ... ");

            await image.SaveAsPngAsync(
                path: fileInfo.FullName,
                encoder: new PngEncoder
                {
                    BitDepth = PngBitDepth.Bit16,
                    ColorType = PngColorType.Rgb,
                    //CompressionLevel = PngCompressionLevel.BestCompression,
                });

            Console.Error.WriteLine("Done.");

            return 0;
        }
    }
}
