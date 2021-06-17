using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RenderSandBox
{
    internal interface IScene
    {
        public Rectangle View { get; }

        public ValueTask Render<TPixel>(
            RenderJob job,
            IProgress<float> progress,
            CancellationToken cancellationToken)
            where TPixel : unmanaged, IPixel<TPixel>;

    }

    internal abstract class SceneBase<TScenePixel> : IScene where TScenePixel : unmanaged, IPixel<TScenePixel>
    {
        private readonly PixelOperations<TScenePixel> _pixelOperations = PixelOperations<TScenePixel>.Instance;
        private readonly Configuration _configuration = Configuration.Default;
        private readonly int _jitter;

        protected SceneBase(
            Rectangle view,
            PixelOperations<TScenePixel>? pixelOperations = null,
            Configuration? configuration = null,
            int jitter = 0)
        {
            View = view;
            _pixelOperations = pixelOperations ?? PixelOperations<TScenePixel>.Instance;
            _configuration = configuration ?? Configuration.Default;
            _jitter = jitter >= 0 ? jitter : throw new ArgumentOutOfRangeException(nameof(jitter));
        }

        public Rectangle View { get; }

        protected abstract ValueTask RenderRow(Rectangle rect, int row, Memory<TScenePixel> buffer);

        public async ValueTask Render<TPixel>(
            RenderJob job,
            IProgress<float> progress,
            CancellationToken cancellationToken)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            var (target, rect) = job;

            using var memOwner = MemoryPool<TScenePixel>.Shared.Rent(rect.Width);
            var rowBuffer = memOwner.Memory[..rect.Width];

            for (var row = 0; row < rect.Height; row++)
            {
                progress.Report((float)row / rect.Height);

                await RenderRow(rect, row, rowBuffer);

                _pixelOperations.To(
                    _configuration,
                    rowBuffer.Span,
                    target.GetPixelRowSpan(rect.Y + row)[rect.X..(rect.X + rect.Width)]);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            if (_jitter > 0)
            {
                await Task.Delay(new Random().Next(_jitter), cancellationToken);
            }
        }
    }
}
