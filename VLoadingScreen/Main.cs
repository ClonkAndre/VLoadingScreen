using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;

using IVSDKDotNet;

namespace VLoadingScreen
{
    internal class Main : Script
    {

        #region Variables and Enums
        // Variables
        private static Random rnd;

        private List<ImageResource> ivBackgroundImages;
        private List<ImageResource> ivCharacterImages;
        private List<ImageResource> tladBackgroundImages;
        private List<ImageResource> tladCharacterImages;
        private List<ImageResource> tbogtBackgroundImages;
        private List<ImageResource> tbogtCharacterImages;

        private CImage[] activeImagesOne, activeImagesTwo;

        private ImageResource[] episodeLogos;
        private bool fadeLogoIn;
        private int fadeAlpha;

        private bool ready;
        private bool canLoadImages;
        private bool canDrawImages;
        private bool ignoreFirstTimerEvent = true;
        private Guid imageSwitchingTimerID;

        private CPosition lastLerpedPosition = CPosition.None;

        // Settings
        public int LoadingScreen_ImageSwitchingInterval;
        public float LoadingScreen_BackgroundLerpSpeed;
        public float LoadingScreen_CharacterLerpSpeed;
        public float LoadingScreen_BackgroundZoomOutSpeed;
        public float LoadingScreen_CharacterMoveSpeed;

        public bool Logo_Show;
        public int  Logo_FadingSpeed;

        // Enums
        private enum CPosition
        {
            None,
            Left,
            Center,
            Right
        }
        #endregion

        #region Classes
        private class AvailableTexture
        {
            #region Variables
            public string FullPath;
            public int Group;
            public string Episode;
            #endregion

            #region Constructor
            public AvailableTexture(string fullPath, int group, string episode)
            {
                FullPath = fullPath;
                Group = group;
                Episode = episode;
            }
            #endregion
        }
        private class ImageResource
        {
            #region Variables
            public IntPtr ImagePtr;
            public Size ImageSize;
            #endregion

            #region Constructor
            public ImageResource(IntPtr ptr, Size size)
            {
                ImagePtr = ptr;
                ImageSize = size;
            }
            #endregion
        }
        private class CImage
        {

            #region Variables and Properties
            // Variables
            private ImageResource resource;
            private bool isBG;

            private SizeF size;
            private SizeF originalSize;
            private Vector2 location;

            private float zoom;
            private float zoomAmount;

            private float moveX;
            private float moveXAmount;

            private bool canLerp;
            private Vector2 lerpPosition;

            private CPosition currentPosition;

            // Properties
            public ImageResource Resource
            {
                get => resource;
                private set => resource = value;
            }

            public SizeF Size
            {
                get => size;
                set => size = value;
            }
            public SizeF OriginalSize
            {
                get => originalSize;
                private set => originalSize = value;
            }
            public Vector2 Location
            {
                get => location;
                set => location = value;
            }
            public RectangleF Rect
            {
                get => new RectangleF(new PointF(Location.X, Location.Y), Size);
            }

            public float Zoom
            {
                get => zoom;
                set => zoom = value;
            }
            public float ZoomAmount
            {
                get => zoomAmount;
                set => zoomAmount = value;
            }

            public float MoveX
            {
                get => moveX;
                set => moveX = value;
            }
            public float MoveXAmount
            {
                get => moveXAmount;
                set => moveXAmount = value;
            }

            public bool CanLerp
            {
                get => canLerp;
                set => canLerp = value;
            }
            public Vector2 LerpPosition
            {
                get => lerpPosition;
                set => lerpPosition = value;
            }

            public CPosition CurrentPosition
            {
                get => currentPosition;
                private set => currentPosition = value;
            }
            #endregion

            #region Constructor
            public CImage(ImageResource res, bool isBackground, CPosition startPosition, float zoomAmount, float moveXSpeed)
            {
                // Get the current resolution of the game
                SizeF resolution = IVGame.Resolution;

                // Get the display dpi
                PointF dpi = Point.Empty;
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) {
                    dpi = new PointF(g.DpiX, g.DpiY);
                }

                // Set stuff
                Resource = res;
                isBG = isBackground;

                // Get and set size from resource
                OriginalSize = res.ImageSize;

                // Set size for background
                if (isBG)
                    Size = new SizeF(resolution.Width - 160f, resolution.Height - 160f);
                else
                    Size = new SizeF(OriginalSize.Width - (dpi.X + 110f), resolution.Height - (dpi.Y + 50f));

                // Set start position
                CurrentPosition = startPosition;
                switch (startPosition)
                {
                    case CPosition.Left:
                        if (isBG)
                            Location = new Vector2(-Size.Width, (resolution.Height / 2f) - (Size.Height / 2f));
                        else
                            Location = new Vector2(-Size.Width, resolution.Height - Size.Height);
                        break;
                    case CPosition.Center:
                        if (isBG)
                            Location = new Vector2((resolution.Width / 2f) - (Size.Width / 2f), (resolution.Height / 2f) - (Size.Height / 2f));
                        else
                            Location = new Vector2((resolution.Width / 2f) - (Size.Width / 2f), resolution.Height - Size.Height);
                        break;
                    case CPosition.Right:
                        if (isBG)
                            Location = new Vector2(resolution.Width + Size.Width, (resolution.Height / 2f) - (Size.Height / 2f));
                        else
                            Location = new Vector2(resolution.Width + Size.Width, resolution.Height - Size.Height);
                        break;
                }

                // Set random things
                ZoomAmount =    Main.rnd.Next(3, 9) / zoomAmount;
                MoveXAmount =   Main.rnd.Next(5, 10) / moveXSpeed;
            }
            #endregion

            #region Methods
            public void SetLerpPosition(CPosition pos)
            {
                SizeF resolution = IVGame.Resolution;
                CurrentPosition = pos;
                switch (pos)
                {
                    case CPosition.Left:
                        if (isBG)
                            LerpPosition = new Vector2(-Size.Width - 100f, (resolution.Height / 2f) - (Size.Height / 2f));
                        else
                            LerpPosition = new Vector2(-Size.Width - 100f, resolution.Height - Size.Height);
                        break;
                    case CPosition.Center:
                        if (isBG)
                            LerpPosition = new Vector2((resolution.Width / 2f) - (Size.Width / 2f), (resolution.Height / 2f) - (Size.Height / 2f));
                        else
                            LerpPosition = new Vector2((resolution.Width / 2f) - (Size.Width / 2f), resolution.Height - Size.Height);
                        break;
                    case CPosition.Right:
                        if (isBG)
                            LerpPosition = new Vector2(resolution.Width + Size.Width + 100f, (resolution.Height / 2f) - (Size.Height / 2f));
                        else
                            LerpPosition = new Vector2(resolution.Width + Size.Width + 100f, resolution.Height - Size.Height);
                        break;
                }
            }
            #endregion

            #region Functions
            public bool IsImageInsideScreenBounds()
            {
                SizeF res = IVGame.Resolution;
                RectangleF resRect = new RectangleF(0f, 0f, res.Width, res.Height);
                return Rect.IntersectsWith(resRect);
            }
            #endregion

        }
        #endregion

        #region Method
        private void StartImageSwitching()
        {
            if (imageSwitchingTimerID != Guid.Empty)
                return;

            canDrawImages = false;
            ignoreFirstTimerEvent = true;
            fadeLogoIn = true;
            fadeAlpha = 0;

            // Start image switching timer
            imageSwitchingTimerID = StartNewTimer(LoadingScreen_ImageSwitchingInterval, () =>
            {

                // Create new random to minimize the chance of image duplication
                rnd = new Random(DateTime.UtcNow.Millisecond);

                // Do stuff on first timer event and return
                if (ignoreFirstTimerEvent)
                {

                    // Set first images for current episode
                    activeImagesOne[0] = new CImage(GetRandomTextureForEpisode(0), true, CPosition.Left, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);       // Background
                    activeImagesOne[1] = new CImage(GetRandomTextureForEpisode(1), false, CPosition.Left, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);     // Character

                    // Set second images for current episode
                    activeImagesTwo[0] = new CImage(GetRandomTextureForEpisode(0), true, CPosition.Right, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);      // Background
                    activeImagesTwo[1] = new CImage(GetRandomTextureForEpisode(1), false, CPosition.Right, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);    // Character

                    // Allow images to be drawn
                    canDrawImages = true;

                    // Lerp first images
                    CImage bgImg = activeImagesOne[0];
                    bgImg.SetLerpPosition(CPosition.Center);
                    bgImg.CanLerp = true;

                    CImage charImg = activeImagesOne[1];
                    charImg.SetLerpPosition(CPosition.Center);
                    charImg.CanLerp = true;

                    ignoreFirstTimerEvent = false;
                    return;
                }

                {
                    CImage bgImg = activeImagesOne[0];
                    CImage charImg = activeImagesOne[1];

                    CImage bgImg2 = activeImagesTwo[0];
                    CImage charImg2 = activeImagesTwo[1];

                    // Lerp currently visible images away
                    if (bgImg.CurrentPosition == CPosition.Center)
                    {
                        // Lerp first images in the opposite way as the second images
                        switch (bgImg2.CurrentPosition)
                        {
                            case CPosition.Left:
                                lastLerpedPosition = CPosition.Right; // Sets in which way the images will be lerped
                                bgImg.SetLerpPosition(CPosition.Right);
                                charImg.SetLerpPosition(CPosition.Right);
                                bgImg.CanLerp = true;
                                charImg.CanLerp = true;
                                break;
                            case CPosition.Right:
                                lastLerpedPosition = CPosition.Left;
                                bgImg.SetLerpPosition(CPosition.Left);
                                charImg.SetLerpPosition(CPosition.Left);
                                bgImg.CanLerp = true;
                                charImg.CanLerp = true;
                                break;
                        }

                    }
                    else
                    {
                        if (bgImg2.CurrentPosition == CPosition.Center)
                        {
                            // Lerp first images in the opposite way as the second images
                            switch (bgImg.CurrentPosition)
                            {
                                case CPosition.Left:
                                    lastLerpedPosition = CPosition.Right; // Sets in which way the images will be lerped
                                    bgImg2.SetLerpPosition(CPosition.Right);
                                    charImg2.SetLerpPosition(CPosition.Right);
                                    bgImg2.CanLerp = true;
                                    charImg2.CanLerp = true;
                                    break;
                                case CPosition.Right:
                                    lastLerpedPosition = CPosition.Left; // Sets in which way the images will be lerped
                                    bgImg2.SetLerpPosition(CPosition.Left);
                                    charImg2.SetLerpPosition(CPosition.Left);
                                    bgImg2.CanLerp = true;
                                    charImg2.CanLerp = true;
                                    break;
                            }
                        }
                    }

                    // Lerp first/second images into view
                    switch (lastLerpedPosition)
                    {
                        case CPosition.Left:

                            // Lerp second images into view
                            bgImg2.SetLerpPosition(CPosition.Center);
                            charImg2.SetLerpPosition(CPosition.Center);
                            bgImg2.CanLerp = true;
                            charImg2.CanLerp = true;

                            break;
                        case CPosition.Right:

                            // Lerp first images into view
                            bgImg.SetLerpPosition(CPosition.Center);
                            charImg.SetLerpPosition(CPosition.Center);
                            bgImg.CanLerp = true;
                            charImg.CanLerp = true;

                            break;
                    }
                }

            });
        }
        private void ChangeImages()
        {
            switch (lastLerpedPosition)
            {
                case CPosition.Left:

                    // Change first images
                    activeImagesOne[0] = new CImage(GetRandomTextureForEpisode(0), true, CPosition.Left, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);       // Background
                    activeImagesOne[1] = new CImage(GetRandomTextureForEpisode(1), false, CPosition.Left, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);     // Character

                    break;
                case CPosition.Right:

                    // Change second images
                    activeImagesTwo[0] = new CImage(GetRandomTextureForEpisode(0), true, CPosition.Right, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);      // Background
                    activeImagesTwo[1] = new CImage(GetRandomTextureForEpisode(1), false, CPosition.Right, LoadingScreen_BackgroundZoomOutSpeed, LoadingScreen_CharacterMoveSpeed);    // Character

                    break;
            }
        }
        #endregion

        #region Functions
        private string GetCurrentEpisodeName()
        {
            switch (IVGame.CurrentEpisodeMenu)
            {
                case 0: return "IV";
                case 1: return "TLAD";
                case 2: return "TBOGT";
            }
            return string.Empty;
        }
        private ImageResource GetRandomTextureForEpisode(int group)
        {
            string episodeName = GetCurrentEpisodeName();
            int rndNum = 0;

            switch (episodeName)
            {
                case "IV":

                    switch (group)
                    {
                        case 0: // backgrounds
                            rndNum = rnd.Next(0, ivBackgroundImages.Count - 1);
                            return ivBackgroundImages[rndNum];
                        case 1: // characters
                            rndNum = rnd.Next(0, ivCharacterImages.Count - 1);
                            return ivCharacterImages[rndNum];
                    }

                    break;
                case "TLAD":

                    switch (group)
                    {
                        case 0: // backgrounds
                            rndNum = rnd.Next(0, tladBackgroundImages.Count - 1);
                            return tladBackgroundImages[rndNum];
                        case 1: // characters
                            rndNum = rnd.Next(0, tladCharacterImages.Count - 1);
                            return tladCharacterImages[rndNum];
                    }

                    break;
                case "TBOGT":

                    switch (group)
                    {
                        case 0: // backgrounds
                            rndNum = rnd.Next(0, tbogtBackgroundImages.Count - 1);
                            return tbogtBackgroundImages[rndNum];
                        case 1: // characters
                            rndNum = rnd.Next(0, tbogtCharacterImages.Count - 1);
                            return tbogtCharacterImages[rndNum];
                    }

                    break;
            }

            return null;
        }
        #endregion

        #region Constructor
        public Main()
        {
            // Local things
            rnd =               new Random(DateTime.UtcNow.Millisecond);
            activeImagesOne =   new CImage[2];
            activeImagesTwo =   new CImage[2];

            ivBackgroundImages =        new List<ImageResource>();
            ivCharacterImages =         new List<ImageResource>();
            tladBackgroundImages =      new List<ImageResource>();
            tladCharacterImages =       new List<ImageResource>();
            tbogtBackgroundImages =     new List<ImageResource>();
            tbogtCharacterImages =      new List<ImageResource>();
            episodeLogos =              new ImageResource[3];

            Initialized +=      Main_Initialized;
            Drawing +=          Main_Drawing;
            GameLoadPriority += Main_GameLoadPriority;
            GameLoad +=         Main_GameLoad;
            MountDevice +=      Main_MountDevice;
            OnFirstD3D9Frame += Main_OnFirstD3D9Frame;
        }
        #endregion

        private void Main_Initialized(object sender, EventArgs e)
        {
            // Load settings
            LoadingScreen_ImageSwitchingInterval =  Settings.GetInteger("LoadingScreen", "ImageSwitchingInterval", 5500);

            LoadingScreen_BackgroundLerpSpeed =     Settings.GetFloat("LoadingScreen", "BackgroundLerpSpeed", 0.04f);
            LoadingScreen_CharacterLerpSpeed =      Settings.GetFloat("LoadingScreen", "CharacterLerpSpeed", 0.04f);

            LoadingScreen_BackgroundZoomOutSpeed =  Settings.GetFloat("LoadingScreen", "BackgroundZoomOutSpeed", 50.0f);
            LoadingScreen_CharacterMoveSpeed =      Settings.GetFloat("LoadingScreen", "CharacterMoveSpeed", 20.0f);

            Logo_Show =         Settings.GetBoolean("Logo", "Show", true);
            Logo_FadingSpeed =  Settings.GetInteger("Logo", "FadingSpeed", 1);
        }
        private void Main_Drawing(object sender, EventArgs e)
        {
            if (!ready && canLoadImages)
            {
                // Load all files
                string[] files = Directory.GetFiles(ScriptResourceFolder, "*.*", SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    string fullPath = files[i];
                    string fileName = Path.GetFileName(fullPath);
                    string fileNameWOExtension = Path.GetFileNameWithoutExtension(fullPath);

                    // Set episode logo if Logo_Show is set to true
                    if (Logo_Show)
                    {
                        if (fileNameWOExtension == "episodeLogo")
                        {
                            bool result = ImGuiIV.CreateTextureFromFile(this, fullPath, out IntPtr ptr, out int width, out int height);

                            if (!result)
                            {
                                IVGame.Console.PrintError(string.Format("[VLoadingScreen] Failed to create texture {0}!", fileName));
                                continue;
                            }

                            if (fullPath.Contains("IV_Resources"))
                            {
                                episodeLogos[0] = new ImageResource(ptr, new Size(width, height));
#if DEBUG
                                IVGame.Console.Print(string.Format("[VLoadingScreen] Added episode logo image {0} to index 0 (IV) in the episodeLogos array.", fileName));
#endif
                            }
                            if (fullPath.Contains("TLAD_Resources"))
                            {
                                episodeLogos[1] = new ImageResource(ptr, new Size(width, height));
#if DEBUG
                                IVGame.Console.Print(string.Format("[VLoadingScreen] Added episode logo image {0} to index 1 (TLAD) in the episodeLogos array.", fileName));
#endif
                            }
                            if (fullPath.Contains("TBOGT_Resources"))
                            {
                                episodeLogos[2] = new ImageResource(ptr, new Size(width, height));
#if DEBUG
                                IVGame.Console.Print(string.Format("[VLoadingScreen] Added episode logo image {0} to index 2 (TBOGT) in the episodeLogos array.", fileName));
#endif
                            }

                            continue;
                        }
                    }

                    // Get group from path
                    int group = -1;
                    if (fullPath.Contains("backgrounds")) group = 0;
                    if (fullPath.Contains("characters")) group = 1;

                    // Get episode from path
                    string episode = "";
                    if (fullPath.Contains("IV_Resources")) episode = "IV";
                    if (fullPath.Contains("TBOGT_Resources")) episode = "TBOGT";
                    if (fullPath.Contains("TLAD_Resources")) episode = "TLAD";

                    // Try to create texture
                    bool result2 = ImGuiIV.CreateTextureFromFile(this, fullPath, out IntPtr ptr2, out int width2, out int height2);

                    if (!result2)
                    {
                        IVGame.Console.PrintError(string.Format("[VLoadingScreen] Failed to create texture {0}! Group: {1}, Episode: {2}", fileName, group, episode));
                        continue;
                    }

                    ImageResource resource = new ImageResource(ptr2, new Size(width2, height2));

#if DEBUG
                    IVGame.Console.Print(string.Format("[VLoadingScreen] Successfully created texture {0}. Group: {1}, Episode: {2}", fileName, group, episode));
#endif

                    // Add texture to right list
                    switch (group)
                    {
                        case 0: // backgrounds

                            switch (episode)
                            {
                                case "IV":
                                    ivBackgroundImages.Add(resource);
#if DEBUG
                                    IVGame.Console.Print(string.Format("[VLoadingScreen] Texture Resource {0} was added to ivBackgroundImages list.", fileName));
#endif
                                    break;
                                case "TBOGT":
                                    tbogtBackgroundImages.Add(resource);
#if DEBUG
                                    IVGame.Console.Print(string.Format("[VLoadingScreen] Texture Resource {0} was added to tbogtBackgroundImages list.", fileName));
#endif
                                    break;
                                case "TLAD":
                                    tladBackgroundImages.Add(resource);
#if DEBUG
                                    IVGame.Console.Print(string.Format("[VLoadingScreen] Texture Resource {0} was added to tladBackgroundImages list.", fileName));
#endif
                                    break;
                                default:
                                    IVGame.Console.PrintError(string.Format("Texture {0} unknown episode {1}. Disposing.", fileName, episode));
                                    ImGuiIV.ReleaseTexture(this, ref resource.ImagePtr);
                                    resource = null;
                                    break;
                            }

                            break;

                        case 1: // characters

                            switch (episode)
                            {
                                case "IV":
                                    ivCharacterImages.Add(resource);
#if DEBUG
                                    IVGame.Console.Print(string.Format("[VLoadingScreen] Texture Resource {0} was added to ivCharacterImages list.", fileName));
#endif
                                    break;
                                case "TBOGT":
                                    tbogtCharacterImages.Add(resource);
#if DEBUG
                                    IVGame.Console.Print(string.Format("[VLoadingScreen] Texture Resource {0} was added to tbogtCharacterImages list.", fileName));
#endif
                                    break;
                                case "TLAD":
                                    tladCharacterImages.Add(resource);
#if DEBUG
                                    IVGame.Console.Print(string.Format("[VLoadingScreen] Texture Resource {0} was added to tladCharacterImages list.", fileName));
#endif
                                    break;
                                default:
                                    IVGame.Console.PrintError(string.Format("Texture {0} unknown episode {1}. Disposing.", episode, group));
                                    ImGuiIV.ReleaseTexture(this, ref resource.ImagePtr);
                                    break;
                            }

                            break;

                        default:
                            IVGame.Console.PrintError(string.Format("Texture {0} unknown group {1}. Disposing.", fileName, group));
                            ImGuiIV.ReleaseTexture(this, ref resource.ImagePtr);
                            resource = null;
                            break;
                    }
                }

                ready = true;
            }
        }

        private void Main_GameLoadPriority(object sender, EventArgs e)
        {
            StartImageSwitching();
        }
        private void Main_GameLoad(object sender, EventArgs e)
        {
            StartImageSwitching();
        }
        private void Main_MountDevice(object sender, EventArgs e)
        {
            // Stop image switching timer and fade out all visible images
            canDrawImages = false;
            AbortTaskOrTimer(imageSwitchingTimerID);
            imageSwitchingTimerID = Guid.Empty;
            ignoreFirstTimerEvent = true;
        }

        private void Main_OnFirstD3D9Frame(object sender, EventArgs e)
        {
            ImGuiIV.AddDrawCommand(this, () =>
            {
                if (ImGuiIV.BeginCanvas(this, out ImGuiIV_DrawingContext ctx))
                {
                    if (ready)
                        DrawStuff(ctx);
                    else
                    {
                        ctx.AddText(new Vector2(100f), Color.Red, 28f, "Creating loading screen textures for VLoadingScreen. Please wait.");
                        canLoadImages = true;
                    }

                    ImGuiIV.EndCanvas();
                }
            });
        }

        private void DrawStuff(ImGuiIV_DrawingContext ctx)
        {
            // Draw images
            if (canDrawImages)
            {
                SizeF resolution = IVGame.Resolution;

                #region First Images
                {
                    CImage bgImg = activeImagesOne[0]; // Background
                    CImage charImg = activeImagesOne[1]; // Character

                    // Background
                    if (bgImg.CanLerp)
                    {
                        bgImg.Location = Vector2.Lerp(bgImg.Location, bgImg.LerpPosition, 0.04f); // 0.025f
                        if (Vector2.Distance(bgImg.Location, bgImg.LerpPosition) <= 1f)
                        {
                            bgImg.CanLerp = false;
                            ChangeImages();
                        }
                    }

                    // Only do zooming and moving stuff when image is actually visible on screen
                    if (bgImg.IsImageInsideScreenBounds())
                        bgImg.Size = new SizeF(bgImg.Size.Width - bgImg.ZoomAmount, bgImg.Size.Height - bgImg.ZoomAmount);

                    // Character
                    if (charImg.CanLerp)
                    {
                        charImg.Location = Vector2.Lerp(charImg.Location, charImg.LerpPosition, 0.04f); // 0.025f
                        if (Vector2.Distance(charImg.Location, charImg.LerpPosition) <= 1.5f) charImg.CanLerp = false;
                    }

                    // Only do zooming and moving stuff when image is actually visible on screen
                    if (charImg.IsImageInsideScreenBounds())
                        charImg.Location = new Vector2(charImg.Location.X + charImg.MoveXAmount, charImg.Location.Y);

                    // Draw stuff
                    ctx.AddImage(bgImg.Resource.ImagePtr, bgImg.Rect, Color.White);
                    ctx.AddImage(charImg.Resource.ImagePtr, charImg.Rect, Color.White);
                }
                #endregion

                #region Second Images
                {
                    CImage bgImg = activeImagesTwo[0]; // Background
                    CImage charImg = activeImagesTwo[1]; // Character

                    // Background
                    if (bgImg.CanLerp)
                    {
                        bgImg.Location = Vector2.Lerp(bgImg.Location, bgImg.LerpPosition, 0.04f); // 0.025f
                        if (Vector2.Distance(bgImg.Location, bgImg.LerpPosition) <= 1f)
                        {
                            bgImg.CanLerp = false;
                            ChangeImages();
                        }
                    }

                    // Only do zooming and moving stuff when image is actually visible on screen
                    if (bgImg.IsImageInsideScreenBounds())
                        bgImg.Size = new SizeF(bgImg.Size.Width - bgImg.ZoomAmount, bgImg.Size.Height - bgImg.ZoomAmount);

                    // Character
                    if (charImg.CanLerp)
                    {
                        charImg.Location = Vector2.Lerp(charImg.Location, charImg.LerpPosition, 0.04f); // 0.025f
                        if (Vector2.Distance(charImg.Location, charImg.LerpPosition) <= 1.5f)
                            charImg.CanLerp = false;
                    }

                    // Only do zooming and moving stuff when image is actually visible on screen
                    if (charImg.IsImageInsideScreenBounds())
                        charImg.Location = new Vector2(charImg.Location.X - charImg.MoveXAmount, charImg.Location.Y);

                    // Draw stuff
                    ctx.AddImage(bgImg.Resource.ImagePtr, bgImg.Rect, Color.White);
                    ctx.AddImage(charImg.Resource.ImagePtr, charImg.Rect, Color.White);
                }
                #endregion

                // Draw episode logo
                if (Logo_Show)
                {
                    ImageResource episodeLogo = episodeLogos[IVGame.CurrentEpisodeMenu];

                    if (episodeLogo != null)
                    {
                        if (fadeLogoIn)
                        {
                            fadeAlpha += Logo_FadingSpeed;
                            if (fadeAlpha >= 255)
                            {
                                fadeAlpha = 255;
                                fadeLogoIn = false;
                            }
                        }

                        Size episodeLogoSize = episodeLogo.ImageSize;
                        ctx.AddImage(episodeLogo.ImagePtr, new RectangleF(30f, (resolution.Height - episodeLogoSize.Height) - 30f, episodeLogoSize.Width, episodeLogoSize.Height), Color.FromArgb(fadeAlpha, Color.White));
                    }
                }
            }
        }

    }
}
