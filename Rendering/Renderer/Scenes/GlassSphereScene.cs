using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    public interface IWorldItem
    {
        /// <summary>
        /// Produce color from the perspective of this item along given ray.
        /// </summary>
        /// <param name="ray">The halfline to trace.</param>
        /// <param name="ignore">Item to ignore.</param>
        /// <returns>
        /// Color as <see cref="Vector4"/>
        /// </returns>
        /// <remarks>
        /// Should delegate to any hit contained items.
        /// May delegate to containing item passing itself as ignore.
        /// </remarks>
        Vector4 GetColor(in HalfLine ray, IWorldItem? ignore = null);

        /// <summary>
        /// Calculate distance to first intersection with this item w.r.t. given ray.
        /// </summary>
        /// <param name="ray">The halfline to trace.</param>
        /// <returns>
        /// Distance to intersection w.r.t. given ray. NaN if no intersection.
        /// </returns>
        float GetIntersection(in HalfLine ray);
    }

    public class World : IWorldItem
    {
        private readonly IWorldItem[] _worldItems;

        public World(IEnumerable<IWorldItem> items) => _worldItems = items.ToArray();

        public World(params IWorldItem[] items) : this(items.AsEnumerable()) { }

        public Vector4 GetColor(in HalfLine ray, IWorldItem? ignore = null)
        {
            IWorldItem? hitItem = null;
            var hitDistance = float.PositiveInfinity;

            for (var i = 0; i < _worldItems.Length; i++)
            {
                var item = _worldItems[i];

                if (ReferenceEquals(item, ignore))
                {
                    continue;
                }

                var distance = item.GetIntersection(ray);

                if (float.IsNaN(distance))
                {
                    continue;
                }

                if (distance < hitDistance)
                {
                    hitItem = item;
                    hitDistance = distance;
                }
            }

            return hitItem is null
                ? Vector4.Zero
                : hitItem.GetColor(ray + hitDistance);
        }

        public float GetIntersection(in HalfLine ray) => float.NaN;
    }

    public class CheckeredHalfVolume : IWorldItem
    {
        private readonly Plane _plane;

        public CheckeredHalfVolume(Plane plane) => _plane = plane;

        public Vector4 GetColor(in HalfLine ray, IWorldItem? ignore = null) => throw new NotImplementedException();

        public float GetIntersection(in HalfLine ray) => throw new NotImplementedException();
    }

    public class GlassSphere : IWorldItem
    {
        private readonly Point _position;
        private readonly float _radius;
        private readonly Vector4 _tint;

        public GlassSphere(Point position, float radius, Vector4 tint)
        {
            _position = position;
            _radius = radius;
            _tint = tint;
        }

        public Vector4 GetColor(in HalfLine ray, IWorldItem? ignore = null)
        {
            var inOnOut = (ray.Start - _position).LengthSquared() - _radius * _radius;

            switch (inOnOut)
            {
                case < 0f:  // Start inside
                    //return GetColorInside(in ray);
                    throw new NotImplementedException();
                case > 0f:  // Start outside
                    throw new NotImplementedException();
                default:    // Start on surface
                    throw new NotImplementedException();
            }
        }

        public float GetIntersection(in HalfLine ray)
        {
            var offset = _position - ray.Start;
            var distByRay = Vector3.Dot(ray.Direction.Value, offset);
            var sectionRadiusSquared = distByRay * distByRay + _radius * _radius - offset.LengthSquared();

            if (sectionRadiusSquared < 0f)  // Missed the ball
            {
                return float.NaN;
            }

            var sectionRadius = MathF.Sqrt(sectionRadiusSquared);
            var firstIntersection = distByRay - sectionRadius;

            return firstIntersection >= 0f ? firstIntersection  // Hit from outside
                : distByRay + sectionRadius < 0f ? float.NaN    // Wrong direction
                : 0f;   // We're inside
        }
    }

    public class DiscLight : IWorldItem
    {
        private readonly Point _position;
        private readonly Direction _direction;
        private readonly Vector4 _color;

        public DiscLight(Point position, Direction direction, Vector4 color)
        {
            _position = position;
            _direction = direction;
            _color = color;
        }

        public Vector4 GetColor(in HalfLine ray, IWorldItem? ignore = null) => throw new NotImplementedException();

        public float GetIntersection(in HalfLine ray) => throw new NotImplementedException();
    }


    public class GlassSphereScene : Scene3DBase
    {
        private readonly World _world;

        public GlassSphereScene(Camera camera) : base(camera)
        {
            var surface = new CheckeredHalfVolume(new Plane(Direction.PositiveY, 0f));
            var sphere = new GlassSphere(new Point(0f, 1f, 0f), 1f, new Vector4(0f, 0f, 1f, 1f));
            var light = new DiscLight(new Point(2f, 4f, -1f), Direction.NegativeY, Vector4.One);
            _world = new World(surface, sphere, light);
        }

        protected override Vector4 GetColor(in HalfLine ray)

            => _world.GetColor(in ray);
    }
}
