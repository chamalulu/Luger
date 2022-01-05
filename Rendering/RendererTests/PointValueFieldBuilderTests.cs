using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Luger.Rendering.Renderer.Utils;

using Xunit;

namespace RendererTests
{
    public class PointValueFieldBuilderTests
    {
        [Fact]
        public async Task ParallelTwoDots()
        {
            // Arrange
            var pointValues = new PointValuePair[]
            {
                new(-2f * Vector3.UnitX, Vector3.UnitX),    // 1 red at -2 x
                new(Vector3.UnitX, Vector3.UnitY)   // 1 blue at 1 x
            };

            var expected = new Vector3(.25f, 1f, 0f);   // .25 red + 1 blue (at O)

            // Act
            using var builder = new PointValueFieldBuilder();
            await Task.WhenAll(pointValues.Select(pv => Task.Run(() => builder.Add(pv))));
            var field = builder.ToImmutable();
            var actual = field[Vector3.Zero];

            // Assert
            Assert.Equal(expected, actual);
            Assert.Equal(2, field.Count);
            Assert.Equal(1, field.Depth);
        }

        [Fact]
        public void Parallel1MDots()
        {
            // Arrange
            var rng = new Random();

            // Act
            using var builder = new PointValueFieldBuilder();

            Parallel.For(
                fromInclusive: 0,
                toExclusive: 1000000,
                parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                body: _ => builder.Add(new(
                    point: new((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()),
                    value: new((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()))));

            var field = builder.ToImmutable();

            // Assert
            Assert.Equal(1000000, field.Count);
            Assert.True(field.Depth > 13);
        }

        [Fact]
        public void Sequential129Dots()
        {
            // Arrange
            var rng = new Random();

            // Act
            using var builder = new PointValueFieldBuilder();

            for (var i = 0; i < 129; i++)
            {
                builder.Add(new(
                    point: new((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()),
                    value: new((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble())));
            }

            var field = builder.ToImmutable();

            // Assert
            Assert.Equal(129, field.Count);
            Assert.True(field.Depth == 2);
        }
    }
}
