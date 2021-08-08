using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Luger.Rendering.Renderer;
using Luger.Rendering.Renderer.Scenes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

using Point = Luger.Rendering.Renderer.Scenes.Point;

namespace RenderSandBox
{
    public class RenderService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RenderService> _logger;
        private readonly IOptions<RenderOptions> _options;

        public RenderService(
            IHostApplicationLifetime applicationLifetime,
            IConfiguration configuration,
            ILogger<RenderService> logger,
            IOptions<RenderOptions> options)
        {
            _applicationLifetime = applicationLifetime;
            _configuration = configuration;
            _logger = logger;
            _options = options;
        }

        private async Task<IScene> CreateScene()
        {
            //IScene scene = new MandelbrotScene
            //{
            //    Palette = new Vector4[] { new(0, 0, 0, 0), new(.5f, .5f, .5f, 1), new(.25f, .5f, .75f, 1), new(.8f, .4f, 0, 1) }
            //};

            var rnd = new Random();
            var cameraPosition = new Point((float)rnd.NextDouble() + 4f, (float)rnd.NextDouble() + 4f, (float)rnd.NextDouble() + 4f);
            var cameraForward = new Direction(Point.Origin - cameraPosition);
            var cameraUp = new Direction(new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble()));
            var camera = new Camera(cameraPosition, cameraForward, cameraUp);
            IScene scene = new BasePlanesScene(camera);

            //IScene scene = new TestScene();
            return scene.SupersampleTwice().GammaCorrect();
        }

        private async Task Main()
        {
            // Produce size of image from options
            var width = _options.Value.Width;
            var height = _options.Value.Height;

            // Create scene, image and renderer
            var scene = await CreateScene();
            var image = new Image<RgbaVector>(width, height);
            var renderer = new Renderer<RgbaVector>(scene, image);

            // Start render task
            var stopWatch = Stopwatch.StartNew();
            var renderTask = renderer.StartRenderTask(_applicationLifetime.ApplicationStopping);

            // Subscribe to render progress, start and await render task.
            using (renderer.GetProgress(
                intervalMs: 500d,
                cancellationToken: _applicationLifetime.ApplicationStopping)
                .Subscribe(
                    onNext: progress =>
                    {
                        var message = new StringBuilder();
                        message.Append("Pixels: ").Append(progress.pixels.ToString("N0")).Append('\t');
                        message.Append("Progress: ").Append(progress.progress.ToString("P2")).Append('\t');

                        var log1000 = (int)MathF.Log10(progress.pixelsPerSecond) / 3;
                        var ppsMantissa = progress.pixelsPerSecond / MathF.Pow(1000f, log1000);

                        message.Append("Rate: ").Append(ppsMantissa.ToString("N2")).Append(' ');

                        if (log1000 > 0)
                        {
                            message.Append(" KMGT"[log1000]);
                        }

                        message.Append("Pps");

                        _logger.LogInformation(message.ToString(), progress);
                    },
                    onError: ex => _logger.LogError(ex, "Progress sequence faulted with the message: {message}", ex.Message)))
            {
                _logger.LogInformation("Render task started. It's in status {status}", renderTask.Status);

                await renderTask;

                stopWatch.Stop();

                _logger.LogInformation("Render task completed. It's in status {status}", renderTask.Status);
            }

            _logger.LogInformation(
                "Finished rendering {width:N0}x{height:N0} pixels in {elapsed} time.",
                image.Width, image.Height, stopWatch.Elapsed);

            // Encode image as PNG and save to file
            var fileInfo = new FileInfo(_options.Value.OutFile);

            _logger.LogInformation("Saving image to {filename} ... ", fileInfo.FullName);

            await image.SaveAsPngAsync(
                path: fileInfo.FullName,
                encoder: new PngEncoder
                {
                    BitDepth = PngBitDepth.Bit16,
                    ColorType = PngColorType.Rgb,
                    //CompressionLevel = PngCompressionLevel.BestCompression,
                },
                cancellationToken: _applicationLifetime.ApplicationStopping);

            _logger.LogInformation("Done.");
            _applicationLifetime.StopApplication();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _applicationLifetime.ApplicationStarted.Register(() => Task.Run(Main, cancellationToken));
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
