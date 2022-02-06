using System;
using System.Numerics;

namespace Luger.Rendering.Renderer
{
    public interface IScene
    {
        RectF ViewArea { get; }

        Vector4 GetColor(Vector2 point, Vector2 size);
    }

    public class SupersamplingGrid4 : IScene
    {
        private readonly IScene _source;

        public SupersamplingGrid4(IScene source) => _source = source;

        public RectF ViewArea => _source.ViewArea;

        public Vector4 GetColor(Vector2 point, Vector2 size)
        {
            var subSize = size * .5f;

            var accumulator = _source.GetColor(point, subSize);

            accumulator += _source.GetColor(point + subSize * Vector2.UnitX, subSize);

            accumulator += _source.GetColor(point + subSize * Vector2.UnitY, subSize);

            accumulator += _source.GetColor(point + subSize, subSize);

            return accumulator * .25f;
        }
    }

    public class GammaCorrection : IScene
    {
        private readonly IScene _source;
        private readonly float _power;

        public GammaCorrection(IScene source, float gamma = 2.2f)
        {
            _source = source;
            _power = 1f / gamma;
        }

        public RectF ViewArea => _source.ViewArea;

        public Vector4 GetColor(Vector2 point, Vector2 size)
        {
            var linear = _source.GetColor(point, size);

            var x = MathF.Pow(linear.X, _power);
            var y = MathF.Pow(linear.Y, _power);
            var z = MathF.Pow(linear.Z, _power);

            return new(x, y, z, linear.W);
        }
    }

    public static class Scene
    {
        public static IScene Supersample(this IScene scene) => new SupersamplingGrid4(scene);

        public static IScene SupersampleTwice(this IScene scene) => scene.Supersample().Supersample();

        public static IScene GammaCorrect(this IScene scene, float gamma = 2.2f) => new GammaCorrection(scene, gamma);
    }
}
