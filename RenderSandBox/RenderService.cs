using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

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
            IScene scene = new TestScene();
            scene = scene.SupersampleTwice();
            return scene;
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

            // Calculate scene area from greatest fit of image rectangle in scene view radius (centered on 0,0)
            var viewRadius = scene.ViewRadius;
            var diag = MathF.Sqrt(width * width + height * height);
            var halfViewWidth = width * viewRadius / diag;
            var halfViewHeight = height * viewRadius / diag;
            var sceneArea = new RectF(-halfViewWidth, -halfViewHeight, 2 * halfViewWidth, 2 * halfViewHeight);

            // Start render
            var stopWatch = Stopwatch.StartNew();

            var renderTask = renderer.Render(sceneArea, _applicationLifetime.ApplicationStopping);

            // Await render complete while subscribing to render progress
            using (renderer.GetProgress().Subscribe(
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
                }))
            {
                await renderTask;
                stopWatch.Stop();
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
