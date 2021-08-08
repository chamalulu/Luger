using System;
using System.Numerics;

namespace Luger.Rendering.Renderer.Scenes
{
    public class DotFieldExperimentBase : Scene3DBase
    {
        public DotFieldExperimentBase(Camera camera) : base(camera) { }

        protected override Vector4 GetColor(in HalfLine ray) => throw new NotImplementedException();
    }
}
