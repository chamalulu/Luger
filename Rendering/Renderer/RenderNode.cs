using System;

namespace Luger.Rendering.Renderer
{
    public readonly struct RectF
    {
        public readonly float X, Y, Width, Height;

        public RectF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public (RectF left, RectF right) SplitHorizontal(float atWidth)
        {
            var left = new RectF(X, Y, atWidth, Height);
            var right = new RectF(X + atWidth, Y, Width - atWidth, Height);
            return (left, right);
        }

        public (RectF up, RectF down) SplitVertical(float atHeight)
        {
            var up = new RectF(X, Y, Width, atHeight);
            var down = new RectF(X, Y + atHeight, Width, Height - atHeight);
            return (up, down);
        }

        public void Deconstruct(out float x, out float y, out float width, out float height)

            => (x, y, width, height) = (X, Y, Width, Height);
    }

    public readonly struct RectI
    {
        public readonly int X, Y, Width, Height;

        public RectI(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public (RectI left, RectI right) SplitHorizontal(int atWidth)
        {
            var left = new RectI(X, Y, atWidth, Height);
            var right = new RectI(X + atWidth, Y, Width - atWidth, Height);
            return (left, right);
        }

        public (RectI up, RectI down) SplitVertical(int atHeight)
        {
            var up = new RectI(X, Y, Width, atHeight);
            var down = new RectI(X, Y + atHeight, Width, Height - atHeight);
            return (up, down);
        }

        public void Deconstruct(out int x, out int y, out int width, out int height)

            => (x, y, width, height) = (X, Y, Width, Height);
    }

    public class RenderNode
    {
        public RectF SceneArea { get; }
        public RectI ImageArea { get; }

        public RenderNode(RectF sceneArea, RectI imageArea)
        {
            if (sceneArea.Width == 0f || sceneArea.Height == 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sceneArea));
            }

            if (imageArea.Width <= 0 || imageArea.Height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(imageArea));
            }

            SceneArea = sceneArea;
            ImageArea = imageArea;
        }

        private (RenderNode left, RenderNode right) SplitHorizontal()
        {
            var leftImageWidth = ImageArea.Width >> 1;
            var leftSceneWidth = SceneArea.Width * leftImageWidth / ImageArea.Width;

            var (leftImageArea, rightImageArea) = ImageArea.SplitHorizontal(leftImageWidth);
            var (leftSceneArea, rightSceneArea) = SceneArea.SplitHorizontal(leftSceneWidth);

            var left = new RenderNode(leftSceneArea, leftImageArea);
            var right = new RenderNode(rightSceneArea, rightImageArea);

            return (left, right);
        }

        private (RenderNode up, RenderNode down) SplitVertical()
        {
            var upImageHeight = ImageArea.Height >> 1;
            var upSceneHeight = SceneArea.Height * upImageHeight / ImageArea.Height;

            var (upImageArea, downImageArea) = ImageArea.SplitVertical(upImageHeight);
            var (upSceneArea, downSceneArea) = SceneArea.SplitVertical(upSceneHeight);

            var up = new RenderNode(upSceneArea, upImageArea);
            var down = new RenderNode(downSceneArea, downImageArea);

            return (up, down);
        }

        public (RenderNode child1Node, RenderNode child2Node) Split()

            => ImageArea.Width > ImageArea.Height
                ? SplitHorizontal()
                : SplitVertical();

        public void Deconstruct(out RectF sceneArea, out RectI imageArea)

            => (sceneArea, imageArea) = (SceneArea, ImageArea);
    }
}
