using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

using VLoadingScreen.Classes;

using IVSDKDotNet;
using IVSDKDotNet.Enums;

namespace VLoadingScreen
{
    internal class Main : Script
    {

        #region Variables
        internal static Main Instance;

        private Queue<Action> renderThreadQueue;
        private List<EpisodeResources> availableEpisodeLoadingScreens;

        public string[] CachedFilesWithinResourcesFolder;

        // Resources
        private EpisodeResources currentEpisodeResources;
        private List<LoadingScreen> currentLoadingTextures;
        private LoadingTexture currentLogoTexture;

        private float backgroundZoom = 1.0f;
        private float characterOffset;
        private float logoTransparency;

        private DateTime addNextLoadingScreenAt;

        // States
        private bool isLoading;
        #endregion

        #region Methods
        private void LoadAvailableLoadingScreens()
        {
            try
            {
                string path = Path.Combine(ScriptResourceFolder, "availableEpisodeLoadingScreens.json");

                if (!File.Exists(path))
                {
                    Logging.LogWarning("Could not load all available episode loading screens because the 'availableEpisodeLoadingScreens.json' file does not exists!");
                    return;
                }

                availableEpisodeLoadingScreens = Helper.ConvertJsonStringToObject<List<EpisodeResources>>(File.ReadAllText(path));
                Logging.Log("Loaded {0} available episode loading screens.", availableEpisodeLoadingScreens.Count);
            }
            catch (Exception ex)
            {
                Logging.LogError("An error occured while trying to load all available episode loading screens! Details: {0}", ex);
            }
        }

        private void PreloadEpisodeLogoTextures()
        {
            renderThreadQueue.Enqueue(() =>
            {
                availableEpisodeLoadingScreens.ForEach(x => x.PreloadTexturesOfType(TextureType.Logo));
            });
        }
        private void ReleaseAllTextures()
        {
            renderThreadQueue.Enqueue(() =>
            {
                availableEpisodeLoadingScreens.ForEach(x => x.ReleaseAllTextures());
            });
        }

        // Adding a new loading screen to the list will trigger a switch
        private void AddNextLoadingScreen(LoadingScreen info)
        {
            // Get first loading screen and tell it to move away
            LoadingScreen first = currentLoadingTextures.FirstOrDefault();

            if (first != null)
            {
                switch (info.InitialStartingPos)
                {
                    case StartingPosition.Left:
                        first.TargetPos = TargetPosition.Right;
                        break;
                    case StartingPosition.Right:
                        first.TargetPos = TargetPosition.Left;
                        break;
                }
            }

            // Add next loading screen
            currentLoadingTextures.Add(info);
        }
        private void AddNextRandomLoadingScreen()
        {
            LoadingTexture backgroundTexture = currentEpisodeResources.GetRandomLoadingTextureByType(TextureType.Background).CreateLoadingTexture();
            LoadingTexture characterTexture = currentEpisodeResources.GetRandomLoadingTextureByType(TextureType.Character).CreateLoadingTexture();

            AddNextLoadingScreen(new LoadingScreen(backgroundTexture, characterTexture));

            addNextLoadingScreenAt = DateTime.UtcNow.AddSeconds(10d);
        }

        private void DrawAndProcessLoadingScreen(ImGuiIV_DrawingContext ctx)
        {
            ImGuiIV_IO io = ImGuiIV.GetIO();

            // Draw background and character
            for (int i = 0; i < currentLoadingTextures.Count; i++)
            {
                LoadingScreen info = currentLoadingTextures[i];

                if (info == null)
                    continue;

                // ====== Process background ======
                LoadingTexture background = info.BackgroundTexture;
                background.Scale = ((io.DisplaySize / background.GetSize()) * backgroundZoom) * 0.78f /*Final Zoom*/;

                // Figure out where the background needs to be
                Vector2 backgroundTargetPos = Vector2.Zero;

                switch (info.TargetPos)
                {
                    case TargetPosition.Center:
                        backgroundTargetPos = io.DisplaySize * 0.5f;
                        break;
                    case TargetPosition.Left:
                        backgroundTargetPos = new Vector2(0f - background.GetSize().X, io.DisplaySize.Y * 0.5f);
                        break;
                    case TargetPosition.Right:
                        backgroundTargetPos = new Vector2(io.DisplaySize.X + background.GetSize().X, io.DisplaySize.Y * 0.5f);
                        break;
                }

                background.Position = Vector2.Lerp(background.Position, backgroundTargetPos, 0.06f);

                background.TopRightCornerOffset -= new Vector2(0.06f, -0.06f); // Move the top-right corner of the image down and to the left
                background.BottomRightCornerOffset -= new Vector2(0.06f, 0.06f); // Move the bottom-right corner of the image up and to the left



                // ====== Process character ======
                LoadingTexture character = info.CharacterTexture;
                Vector2 charSize = character.GetSize();
                character.Scale = new Vector2((io.DisplaySize.X / charSize.X) * 0.5f, io.DisplaySize.Y / charSize.Y) * 0.85f /*Final Zoom*/;

                // Figure out where the character needs to be
                Vector2 characterTargetPos = Vector2.Zero;

                switch (info.TargetPos)
                {
                    case TargetPosition.Center:
                        characterTargetPos = new Vector2((io.DisplaySize.X * 0.5f) + characterOffset, io.DisplaySize.Y);
                        break;
                    case TargetPosition.Left:
                        characterTargetPos = new Vector2(0f - charSize.X, io.DisplaySize.Y);
                        break;
                    case TargetPosition.Right:
                        characterTargetPos = new Vector2(io.DisplaySize.X + charSize.X, io.DisplaySize.Y);
                        break;
                }

                character.Position = Vector2.Lerp(character.Position, characterTargetPos, 0.06f);
                




                // Draw stuff
                background.Draw(ctx, Color.White);
                character.Draw(ctx, Color.White);

                // Move char and zoom out bg
                backgroundZoom -= 0.00002f;
                characterOffset -= 0.1f;

                // Remove this loading screen if background has reached its target position
                if (info.TargetPos != TargetPosition.Center && Vector2.Distance(background.Position, backgroundTargetPos) < 1f)
                {
                    currentLoadingTextures.RemoveAt(i);
                    i--;
                }
            }

            // Draw logo
            if (currentLogoTexture != null)
            {
                logoTransparency = logoTransparency.Lerp(255f, 0.1f);

                currentLogoTexture.Position = new Vector2(10f, (io.DisplaySize.Y - currentLogoTexture.GetSize().Y) - 10f);
                currentLogoTexture.Draw(ctx, Color.FromArgb((int)logoTransparency, Color.White));
            }
        }
        #endregion

        #region Functions
        private EpisodeResources GetEpisodeResourcesForEpisode(int episode)
        {
            return availableEpisodeLoadingScreens.Where(x => x.EpisodeID == episode).FirstOrDefault();
        }
        #endregion

        #region Constructor
        public Main()
        {
            Instance = this;

            renderThreadQueue = new Queue<Action>();
            availableEpisodeLoadingScreens = new List<EpisodeResources>(3);

            currentLoadingTextures = new List<LoadingScreen>(2);

            // IV-SDK .NET
            Uninitialize +=     Main_Uninitialize;
            Initialized +=      Main_Initialized;
            GameLoadPriority += Main_GameLoadPriority;
            MountDevice +=      Main_MountDevice;
            OnImGuiRendering += Main_OnImGuiRendering;
        }
        #endregion

        private void Main_Uninitialize(object sender, EventArgs e)
        {

        }
        private void Main_Initialized(object sender, EventArgs e)
        {
            ModSettings.Load(Settings);
            LoadAvailableLoadingScreens();

            if (availableEpisodeLoadingScreens.Count != 0)
            {
                PreloadEpisodeLogoTextures();

                // Cache files so we dont need to ask the OS to give us all the files within a directory all the time
                CachedFilesWithinResourcesFolder = Directory.GetFiles(ScriptResourceFolder, "*.*", SearchOption.AllDirectories);
            }
        }

        private void Main_GameLoadPriority(object sender, EventArgs e)
        {
            // Create images for episode
            currentEpisodeResources = GetEpisodeResourcesForEpisode((int)IVGame.CurrentEpisodeMenu);

            if (currentEpisodeResources != null)
            {
                isLoading = true;

                while (!currentEpisodeResources.CreateAllTextures())
                {
                    IVGrcWindow.ProcessWindowMessage();
                    Thread.Sleep(1);
                }

                AddNextRandomLoadingScreen();
            }
        }
        private void Main_MountDevice(object sender, EventArgs e)
        {
            isLoading = false;
            ReleaseAllTextures();
        }

        private void Main_OnImGuiRendering(IntPtr devicePtr, ImGuiIV_DrawingContext ctx)
        {
            while (renderThreadQueue.Count != 0)
            {
                renderThreadQueue.Dequeue().Invoke();
            }

            if (isLoading)
            {
                ImGuiIV_IO io = ImGuiIV.GetIO();
                ctx.AddRectFilled(Vector2.Zero, io.DisplaySize, Color.Black, 0.0f, eImDrawFlags.None);

                // Get logo
                if (currentLogoTexture == null)
                    currentLogoTexture = currentEpisodeResources.GetLoadingTexturesByType(TextureType.Logo).FirstOrDefault().CreateLoadingTexture();

                DateTime dtNow = DateTime.UtcNow;
                if (dtNow > addNextLoadingScreenAt && addNextLoadingScreenAt != DateTime.MinValue)
                {
                    AddNextRandomLoadingScreen();
                }

                DrawAndProcessLoadingScreen(ctx);
            }
        }

    }
}
