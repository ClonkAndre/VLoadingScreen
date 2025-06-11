using System;
using System.Drawing;
using System.Numerics;

using IVSDKDotNet;

namespace VLoadingScreen.Classes
{
    internal class TextureResource
    {

        #region Variables
        private ImTexture theTexture;
        public TextureType Type;
        public string FileName;
        #endregion

        #region Constructor
        public TextureResource(ImTexture texture, TextureType type, string fileName)
        {
            theTexture = texture;
            Type = type;
            FileName = fileName;
        }
        #endregion

        public bool Release()
        {
            if (theTexture == null)
                return false;

            return theTexture.Release();
        }

        public IntPtr GetTexture()
        {
            if (theTexture == null)
                return IntPtr.Zero;
            
            return theTexture.GetTexture();
        }
        public Vector2 GetSize()
        {
            if (theTexture == null)
                return Vector2.Zero;

            SizeF size = theTexture.GetSize();
            return new Vector2(size.Width, size.Height);
        }
        public float GetAspectRatio()
        {
            if (theTexture == null)
                return 0f;

            return theTexture.GetAspectRatio();
        }

        public LoadingTexture CreateLoadingTexture()
        {
            return new LoadingTexture(this);
        }

    }
}
