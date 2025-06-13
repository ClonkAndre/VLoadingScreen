using System;
using System.Drawing;
using System.Numerics;

using IVSDKDotNet;

namespace VLoadingScreen.Classes
{
    internal class LoadingTexture
    {

        #region Variables
        private TextureResource theTextureResource;

        public TextureOriginPosition OriginPosition;

        public Vector2 Scale = Vector2.One;
        public Vector2 Position;

        public Vector2 TopLeftCornerOffset;
        public Vector2 BottomLeftCornerOffset;
        public Vector2 TopRightCornerOffset;
        public Vector2 BottomRightCornerOffset;
        #endregion

        #region Constructor
        public LoadingTexture(TextureResource textureResource)
        {
            theTextureResource = textureResource;

            // Set stuff based on the type
            switch (theTextureResource.Type)
            {
                case TextureType.Background:
                    OriginPosition = TextureOriginPosition.Center;

                    // Set stuff from config
                    if (textureResource.BackgroundTextureConfig != null)
                    {
                        TopLeftCornerOffset = textureResource.BackgroundTextureConfig.TopLeftCornerOffset;
                        BottomLeftCornerOffset = textureResource.BackgroundTextureConfig.BottomLeftCornerOffset;
                        TopRightCornerOffset = textureResource.BackgroundTextureConfig.TopRightCornerOffset;
                        BottomRightCornerOffset = textureResource.BackgroundTextureConfig.BottomRightCornerOffset;
                        Logging.LogDebug("Setting corner offsets for image {0}. {1} - {2} - {3} - {4}", textureResource.FileName, TopLeftCornerOffset, BottomLeftCornerOffset, TopRightCornerOffset, BottomRightCornerOffset);
                    }

                    break;
                case TextureType.Character:
                    OriginPosition = TextureOriginPosition.BottomCenter;
                    break;
                case TextureType.Logo:
                    OriginPosition = TextureOriginPosition.TopLeft;
                    break;
            }
        }
        #endregion

        #region Methods
        public void Draw(ImGuiIV_DrawingContext ctx, Color color)
        {
            IntPtr texturePtr = theTextureResource.GetTexture();

            if (texturePtr == IntPtr.Zero)
                return;

            Vector2 size = theTextureResource.GetSize();

            // Calculate origin pos
            Vector2 p0 = Vector2.Zero;
            Vector2 p1 = Vector2.Zero;
            Vector2 p2 = Vector2.Zero;
            Vector2 p3 = Vector2.Zero;

            switch (OriginPosition)
            {
                case TextureOriginPosition.TopLeft:

                    // Define the quad from top-left corner
                    p0 = Vector2.Zero + TopLeftCornerOffset; // TL
                    p1 = new Vector2(0, size.Y) + BottomLeftCornerOffset; // BL
                    p2 = new Vector2(size.X, 0) + TopRightCornerOffset; // TR
                    p3 = new Vector2(size.X, size.Y) + BottomRightCornerOffset; // BR

                    break;
                case TextureOriginPosition.Center:

                    Vector2 halfSize = new Vector2(size.X, size.Y) * 0.5f;

                    // Define the quad centered at (0,0)
                    p0 = new Vector2(-halfSize.X, -halfSize.Y) + TopLeftCornerOffset; // TL
                    p1 = new Vector2(-halfSize.X, halfSize.Y) + BottomLeftCornerOffset; // BL
                    p2 = new Vector2(halfSize.X, -halfSize.Y) + TopRightCornerOffset; // TR
                    p3 = new Vector2(halfSize.X, halfSize.Y) + BottomRightCornerOffset; // BR

                    break;
                case TextureOriginPosition.BottomCenter:

                    Vector2 halfWidth = new Vector2(size.X * 0.5f, 0f);

                    p0 = new Vector2(-halfWidth.X, -size.Y) + TopLeftCornerOffset; // TL
                    p1 = new Vector2(-halfWidth.X, 0) + BottomLeftCornerOffset;    // BL
                    p2 = new Vector2(halfWidth.X, -size.Y) + TopRightCornerOffset; // TR
                    p3 = new Vector2(halfWidth.X, 0) + BottomRightCornerOffset;    // BR

                    break;
            }

            // Apply Scale
            p0 *= Scale;
            p1 *= Scale;
            p2 *= Scale;
            p3 *= Scale;

            // Translate to desired screen position
            p0 += Position;
            p1 += Position;
            p2 += Position;
            p3 += Position;

            // Texture UVs
            Vector2 uv0 = new Vector2(0f, 0f);
            Vector2 uv1 = new Vector2(0f, 1f);
            Vector2 uv2 = new Vector2(1f, 0f);
            Vector2 uv3 = new Vector2(1f, 1f);

            // Triangle 1: TL -> TR -> BL
            ctx.AddImageQuad(
                texturePtr,
                p0, p1, p2, p2,
                uv0, uv1, uv2, uv2, color
            );

            // Triangle 2: TR -> BR -> BL
            ctx.AddImageQuad(
                texturePtr,
                p1, p3, p2, p2,
                uv1, uv3, uv2, uv2, color
            );

            // Hide aliasing artifacts (or texture aliasing) along the edges
            if (theTextureResource.Type == TextureType.Background)
            {
                ctx.AddLine(p0 - new Vector2(0f, 2f), p1, Color.Black, 5f);
                ctx.AddLine(p1, p3, Color.Black, 5f);
                ctx.AddLine(p3, p2, Color.Black, 5f);
                ctx.AddLine(p2, p0, Color.Black, 5f);
            }

#if false
            //ctx.AddText(actualPosition, Color.Red, string.Format("W:{0},H:{1}", size.Width, size.Height));

            //switch (OriginPosition)
            //{
            //    case TextureOriginPosition.Center:
            //        actualPosition = actualPosition + ((new Vector2(size.Width, size.Height) * 0.5f) * Scale);
            //        break;
            //}

            //ctx.AddRectFilled(actualPosition - new Vector2(6f), actualPosition + new Vector2(6f), Color.Red, 0f, eImDrawFlags.None);

            ctx.AddLine(p0, p1, Color.Red, 1f);
            ctx.AddLine(p1, p3, Color.Green, 1f);
            ctx.AddLine(p3, p2, Color.Blue, 1f);
            ctx.AddLine(p2, p0, Color.Yellow, 1f);
#endif
        }
        #endregion

        #region Functions
        public TextureResource GetTheTextureResource()
        {
            return theTextureResource;
        }
        public Vector2 GetSize()
        {
            if (theTextureResource == null)
                return Vector2.Zero;

            return theTextureResource.GetSize();
        }
        #endregion

    }
}
