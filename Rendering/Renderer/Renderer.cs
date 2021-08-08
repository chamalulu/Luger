using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Luger.Rendering.Renderer
{
    public class Renderer<TPixel> where TPixel : unmanaged, IPixel<TPixel>
    {
        private int _availableTasks;
        private int _pixelsComplete = 0;
        private readonly object _lock = new();
        private Task _renderTask = Task.CompletedTask;
        private CancellationToken _cancellationToken;

        public Renderer(IScene scene, Image<TPixel> image, int maxConcurrency)
        {
            if (maxConcurrency < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            }

            _availableTasks = maxConcurrency;
            Scene = scene;
            Image = image;
        }

        public Renderer(IScene scene, Image<TPixel> image) : this(scene, image, Environment.ProcessorCount - 1) { }

        public IScene Scene { get; }

        public Image<TPixel> Image { get; }

        public IObservable<(int pixels, float progress, float pixelsPerSecond)> GetProgress(
            double intervalMs = 1000d,
            CancellationToken cancellationToken = default,
            IScheduler? scheduler = null)
        {
            var slexiPlatoT = 1f / (Image.Width * Image.Height);
            var interval = TimeSpan.FromMilliseconds(intervalMs);

            static (int pixels, float progress, float pixelsPerSecond) calculatePPS(
                IList<Timestamped<(int pixels, float progress)>> buffer)
            {
                var previous = buffer.Count > 1 ? buffer[^2] : default;
                var current = buffer[^1];
                var deltaPixels = current.Value.pixels - previous.Value.pixels;
                var deltaSeconds = (float)(current.Timestamp - previous.Timestamp).TotalSeconds;
                var pixelsPerSecond = deltaPixels / deltaSeconds;
                return (current.Value.pixels, current.Value.progress, pixelsPerSecond);
            }

            return Observable
                .Generate(
                    initialState: this,
                    condition: r => !cancellationToken.IsCancellationRequested,
                    iterate: r => r,
                    resultSelector: r => (r._pixelsComplete, r._pixelsComplete * slexiPlatoT),
                    timeSelector: _ => interval,
                    scheduler: scheduler ?? TaskPoolScheduler.Default)
                .Timestamp()
                .Buffer(2, 1)
                .Select(calculatePPS);
        }

        public Task StartRenderTask(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_renderTask.IsCompleted)
                {
                    throw new InvalidOperationException();
                }

                _pixelsComplete = 0;
                _cancellationToken = cancellationToken;

                // Make scene area contain the scene view area with maintained aspect ratio and orientation
                var viewArea = Scene.ViewArea;

                // Create transformation from largest centered square in image to largest centered square in scene view area

                // Translate from center of image to 0
                var transformation = Matrix3x2.CreateTranslation(Image.Width / -2f, Image.Height / -2f);

                // Scale from image to unit square maintaining aspect ratio
                var imageAreaScale = Image.Width > Image.Height
                    ? 1f / Image.Height
                    : 1f / Image.Width;

                transformation *= Matrix3x2.CreateScale(imageAreaScale);

                // Scale from unit square to view area maintaining aspect ratio and orientation
                var viewAreaScales = MathF.Abs(viewArea.Width) > MathF.Abs(viewArea.Height)
                    ? new Vector2(MathF.Abs(viewArea.Height) * MathF.Sign(viewArea.Width), viewArea.Height)
                    : new Vector2(viewArea.Width, MathF.Abs(viewArea.Width) * MathF.Sign(viewArea.Height));

                transformation *= Matrix3x2.CreateScale(viewAreaScales);

                // Translate from 0 to center of scene view area
                transformation *= Matrix3x2.CreateTranslation(viewArea.X + viewArea.Width / 2, viewArea.Y + viewArea.Height / 2);

                // Transform image resolution to scene area
                var xy = Vector2.Transform(Vector2.Zero, transformation);
                var wh = Vector2.Transform(new(Image.Width, Image.Height), transformation) - xy;

                var renderNode = new RenderNode(new RectF(xy.X, xy.Y, wh.X, wh.Y), new RectI(0, 0, Image.Width, Image.Height));

                /* Render typically does not yield before first synchronous partition (of size ~ 1/maxConcurrency) is finished,
                 * So we schedule it on a worker thread.
                 */
                _renderTask = Task.Run(() => Render(renderNode).AsTask(), cancellationToken);

                return _renderTask;
            }
        }

        private ValueTask RenderPixel(RenderNode renderNode)
        {
            var ((sx, sy, sw, sh), (ix, iy, _, _)) = renderNode;

            var point = new Vector2(sx, sy);
            var size = new Vector2(sw, sh);

            TPixel pixel = default;
            pixel.FromScaledVector4(Scene.GetColor(point, size));
            Image[ix, iy] = pixel;

            _ = Interlocked.Increment(ref _pixelsComplete);

            return ValueTask.CompletedTask;
        }

        private async ValueTask RenderImage(RenderNode renderNode)
        {
            var (child1Node, child2Node) = renderNode.Split();

            if (Interlocked.Decrement(ref _availableTasks) < 0)
            {
                // Render child nodes in sequence
                _ = Interlocked.Increment(ref _availableTasks);

                await Render(child1Node).ConfigureAwait(false);

                await Render(child2Node).ConfigureAwait(false);
            }
            else
            {
                // Render child nodes in parallel
                var task1 = Task.Run(() => Render(child1Node).AsTask(), _cancellationToken);
                var task2 = Task.Run(() => Render(child2Node).AsTask(), _cancellationToken);

                _ = await Task.WhenAny(task1, task2).ConfigureAwait(false);

                _ = Interlocked.Increment(ref _availableTasks);

                await Task.WhenAll(task1, task2).ConfigureAwait(false);
            }
        }

        private ValueTask Render(RenderNode renderNode)

            => renderNode.ImageArea is { Width: 1, Height: 1 }
                ? RenderPixel(renderNode)
                : RenderImage(renderNode);
    }
}
