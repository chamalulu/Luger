using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace RenderSandBox
{
    public interface IScene
    {
        float ViewRadius { get; }

        ValueTask<Vector4> GetColor(Vector2 point, Vector2 size, CancellationToken cancellationToken);
    }

    public class SupersamplingGrid4 : IScene
    {
        private readonly IScene _source;

        public SupersamplingGrid4(IScene source) => _source = source;

        public float ViewRadius => _source.ViewRadius;

        public async ValueTask<Vector4> GetColor(Vector2 point, Vector2 size, CancellationToken cancellationToken)
        {
            var subSize = size * .5f;

            var accumulator = await _source.GetColor(point, subSize, cancellationToken).ConfigureAwait(false);

            var subPoint = point + subSize * Vector2.UnitX;
            accumulator += await _source.GetColor(subPoint, subSize, cancellationToken).ConfigureAwait(false);

            subPoint = point + subSize * Vector2.UnitY;
            accumulator += await _source.GetColor(subPoint, subSize, cancellationToken).ConfigureAwait(false);

            subPoint = point + subSize;
            accumulator += await _source.GetColor(subPoint, subSize, cancellationToken).ConfigureAwait(false);

            return accumulator * .25f;
        }
    }

    public static class Scene
    {
        public static IScene Supersample(this IScene scene) => new SupersamplingGrid4(scene);

        public static IScene SupersampleTwice(this IScene scene) => new SupersamplingGrid4(new SupersamplingGrid4(scene));
    }
}
