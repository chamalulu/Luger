using System;
using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    public class Camera
    {
        private readonly float _distanceToScreen;
        private readonly Matrix4x4 _viewMatrix;
        private readonly Matrix4x4 _worldMatrix;

        public Point Position { get; }
        public Direction Forward { get; }
        public Direction Up { get; }
        public float FieldOfView { get; }

        public Camera(in Point position, in Direction forward, in Direction up, float fieldOfView = MathF.PI / 3f)
        {
            _distanceToScreen = 1f / MathF.Tan(fieldOfView / 2f);

            _viewMatrix =
                Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, 1f, _distanceToScreen, float.PositiveInfinity) *
                Matrix4x4.CreateLookAt(position.Value, forward.Value, up.Value);

            _worldMatrix = Matrix4x4.CreateWorld(position.Value, forward.Value, up.Value);

            Position = position;
            Forward = forward;
            Up = up;
            FieldOfView = fieldOfView;
        }

        public HalfLine RayFromViewPoint(Vector2 point)
        {
            var start = new Point(point.X, point.Y, -_distanceToScreen) * _worldMatrix;
            var direction = new Direction(start - Position);
            return new HalfLine(start, direction);
        }

        public Vector2 ViewPointFromPoint(in Point point)
        {
            var viewPoint = point * _viewMatrix;
            return new(viewPoint.Value.X, viewPoint.Value.Y);
        }
    }
}
