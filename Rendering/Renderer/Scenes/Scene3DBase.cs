using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    public abstract class Scene3DBase : IScene
    {
        public Camera Camera { get; }

        protected Scene3DBase(Camera camera) => Camera = camera;

        public virtual RectF ViewArea { get; } = new(-1f, -1f, 2f, 2f);

        protected abstract Vector4 GetColor(in HalfLine ray);

        public Vector4 GetColor(Vector2 point, Vector2 size)

            => GetColor(Camera.RayFromViewPoint(point));
    }
}
