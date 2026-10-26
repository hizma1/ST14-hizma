using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    public sealed class StripeBack : Container
    {
        private const float PadSize = 4;
        private const float EdgeSize = 2;
        private static readonly Color EdgeColor = Color.FromHex("#525252ff");

        private bool _hasTopEdge = true;
        private bool _hasBottomEdge = true;
        private bool _hasMargins = true;

        public const string StylePropertyBackground = "background";

        public bool HasTopEdge
        {
            get => _hasTopEdge;
            set
            {
                _hasTopEdge = value;
                InvalidateMeasure();
            }
        }

        public bool HasBottomEdge
        {
            get => _hasBottomEdge;
            set
            {
                _hasBottomEdge = value;
                InvalidateMeasure();
            }
        }

        public bool HasMargins
        {
            get => _hasMargins;
            set
            {
                _hasMargins = value;
                InvalidateMeasure();
            }
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var padSize = HasMargins ? PadSize : 0;
            var padSizeTotal = 0f;

            if (HasBottomEdge)
                padSizeTotal += padSize + EdgeSize;
            if (HasTopEdge)
                padSizeTotal += padSize + EdgeSize;

            var size = Vector2.Zero;

            availableSize.Y -= padSizeTotal;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                size = Vector2.Max(size, child.DesiredSize);
            }

            return size + new Vector2(0, padSizeTotal);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var box = new UIBox2(Vector2.Zero, finalSize);

            var padSize = HasMargins ? PadSize : 0;

            if (HasTopEdge)
            {
                box += (0, padSize + EdgeSize, 0, 0);
            }

            if (HasBottomEdge)
            {
                box += (0, 0, 0, -(padSize + EdgeSize));
            }

            foreach (var child in Children)
            {
                child.Arrange(box);
            }

            return finalSize;
        }


        protected override void Draw(DrawingHandleScreen handle)
        {
            // ST14-start: work in plain floats, with a fractional UI scale the edges could cross and UIBox2 throws
            var padSize = HasMargins ? PadSize : 0;
            var top = 0f;
            var bottom = (float) PixelHeight;

            if (HasTopEdge)
            {
                top = MathF.Min((padSize + EdgeSize) * UIScale, bottom);
                handle.DrawRect(new UIBox2(0, MathF.Min(padSize * UIScale, top), PixelWidth, top), EdgeColor);
            }

            if (HasBottomEdge)
            {
                var edgeBottom = MathF.Max(top, PixelHeight - padSize * UIScale);
                bottom = MathF.Max(top, PixelHeight - (padSize + EdgeSize) * UIScale);
                handle.DrawRect(new UIBox2(0, bottom, PixelWidth, MathF.Max(bottom, edgeBottom)), EdgeColor);
            }

            GetActualStyleBox()?.Draw(handle, new UIBox2(0, top, PixelWidth, bottom), UIScale);
            // ST14-end
        }

        private StyleBox? GetActualStyleBox()
        {
            return TryGetStyleProperty(StylePropertyBackground, out StyleBox? box) ? box : null;
        }
    }
}
