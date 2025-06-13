using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;

using Newtonsoft.Json;

using VLoadingScreen.Classes;
using VLoadingScreen.Classes.Json;

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

        private float logoTransparency;

        private DateTime addNextLoadingScreenAt;

        // States
        private bool isLoading;
        private bool canShowLoadingScreen;
        #endregion

        #region Methods
        private void Cleanup()
        {
            isLoading = false;
            canShowLoadingScreen = false;

            ReleaseAllTextures(TextureType.All);

            renderThreadQueue.Clear();
            availableEpisodeLoadingScreens.Clear();
            currentLoadingTextures.Clear();
        }
        private void Reset()
        {
            isLoading = false;
            canShowLoadingScreen = false;
            addNextLoadingScreenAt = DateTime.MinValue;

            currentEpisodeResources = null;
            currentLogoTexture = null;

            currentLoadingTextures.Clear();

            logoTransparency = 0.0f;

            ReleaseAllTextures(TextureType.Background);
            ReleaseAllTextures(TextureType.Character);
        }

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

                // Read file content
                string content = File.ReadAllText(path);

                // Remove all comments
                content = string.Join(Environment.NewLine,
                    content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                    .Where(line => !line.TrimStart().StartsWith("//")));

                availableEpisodeLoadingScreens = JsonConvert.DeserializeObject<List<EpisodeResources>>(content);
                Logging.Log("Loaded {0} available episode loading screens.", availableEpisodeLoadingScreens.Count);
            }
            catch (Exception ex)
            {
                Logging.LogError("An error occured while trying to load all available episode loading screens! Details: {0}", ex);
            }
        }

        private void InitAllEpisodeResources()
        {
            availableEpisodeLoadingScreens.ForEach(x => x.Init());
        }
        private void CreateAllEpisodeLogoTextures()
        {
            renderThreadQueue.Enqueue(() =>
            {
                availableEpisodeLoadingScreens.ForEach(x => x.CreateTexturesOfType(TextureType.Logo));
            });
        }
        private void ReleaseAllTextures(TextureType ofType)
        {
            renderThreadQueue.Enqueue(() =>
            {
                availableEpisodeLoadingScreens.ForEach(x => x.ReleaseAllTextures(ofType));
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

            addNextLoadingScreenAt = DateTime.UtcNow.AddSeconds(ModSettings.SwitchingInterval);
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

                // =================================
                // ======= Process background ======
                // =================================
                LoadingTexture background = info.BackgroundTexture;
                background.Scale = ((io.DisplaySize / background.GetSize()) * info.BackgroundZoom) * ModSettings.BackgroundDefaultScale /*Final Zoom*/;

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

                background.Position = Vector2.Lerp(background.Position, backgroundTargetPos, ModSettings.LerpAmount);

                background.TopRightCornerOffset     -= new Vector2(0.06f, -0.06f) * ModSettings.PerspectiveChangeSpeedMultiplier /*Speed Multiplier*/; // Move the top-right corner of the image down and to the left
                background.BottomRightCornerOffset  -= new Vector2(0.06f, 0.06f) * ModSettings.PerspectiveChangeSpeedMultiplier /*Speed Multiplier*/; // Move the bottom-right corner of the image up and to the left

                // Zoom bg out
                info.BackgroundZoom -= ModSettings.ZoomOutAmount;

                // =================================
                // ======= Process character =======
                // =================================
                LoadingTexture character = info.CharacterTexture;
                Vector2 charSize = character.GetSize();
                character.Scale = (new Vector2((io.DisplaySize.X / charSize.X) * 0.5f, io.DisplaySize.Y / charSize.Y) * currentEpisodeResources.CharacterScale) * ModSettings.CharacterDefaultScale /*Final Zoom*/;

                // Figure out where the character needs to be
                Vector2 characterTargetPos = Vector2.Zero;

                switch (info.TargetPos)
                {
                    case TargetPosition.Center:
                        characterTargetPos = new Vector2((io.DisplaySize.X * 0.5f) + info.CharacterOffsetX, io.DisplaySize.Y);
                        break;
                    case TargetPosition.Left:
                        characterTargetPos = new Vector2(0f - charSize.X, io.DisplaySize.Y);
                        break;
                    case TargetPosition.Right:
                        characterTargetPos = new Vector2(io.DisplaySize.X + charSize.X, io.DisplaySize.Y);
                        break;
                }

                character.Position = Vector2.Lerp(character.Position, characterTargetPos, ModSettings.LerpAmount);

                // Move char
                info.CharacterOffsetX = info.CharacterMoveDirection == StartingPosition.Left ? info.CharacterOffsetX - ModSettings.CharacterMoveAmount : info.CharacterOffsetX + ModSettings.CharacterMoveAmount;



                // Draw stuff
                background.Draw(ctx, Color.White);
                character.Draw(ctx, Color.White);

                // Remove this loading screen if background has reached its target position
                if (info.TargetPos != TargetPosition.Center && Vector2.Distance(background.Position, backgroundTargetPos) < 1f)
                {
                    currentLoadingTextures.RemoveAt(i);
                    i--;
                }
            }
        }
        private void DrawAndProcessLoadingScreenLogo(ImGuiIV_DrawingContext ctx)
        {
            // Draw logo
            if (ModSettings.ShowLogo && currentLogoTexture != null)
            {
                ImGuiIV_IO io = ImGuiIV.GetIO();
                logoTransparency = logoTransparency.Lerp(255f, ModSettings.LogoFadingSpeed);

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
            Cleanup();
        }
        private void Main_Initialized(object sender, EventArgs e)
        {
            ModSettings.Load(Settings);
            LoadAvailableLoadingScreens();

            if (availableEpisodeLoadingScreens.Count != 0)
            {
                // Cache files so we dont need to ask the OS to give us all the files within a directory all the time
                CachedFilesWithinResourcesFolder = Directory.GetFiles(ScriptResourceFolder, "*.*", SearchOption.AllDirectories);

                InitAllEpisodeResources();
                CreateAllEpisodeLogoTextures();
            }
        }

        private void Main_GameLoadPriority(object sender, EventArgs e)
        {
            // Create images for episode
            currentEpisodeResources = GetEpisodeResourcesForEpisode((int)IVGame.CurrentEpisodeMenu);

            if (currentEpisodeResources != null)
            {
                isLoading = true;

                // Load textures and read their config file
                currentEpisodeResources.CreateAllTextures();
                currentEpisodeResources.ReadAllTextureConfigFiles();

                canShowLoadingScreen = true;

                // Show random loading screen
                AddNextRandomLoadingScreen();
            }
        }
        private void Main_MountDevice(object sender, EventArgs e)
        {
            if (isLoading)
            {
                Reset();
            }
        }

        private void Main_OnImGuiRendering(IntPtr devicePtr, ImGuiIV_DrawingContext ctx)
        {
            while (renderThreadQueue.Count != 0)
            {
                renderThreadQueue.Dequeue().Invoke();
            }

            // Skip logic when we are not loading
            if (!isLoading)
                return;

            ImGuiIV_IO io = ImGuiIV.GetIO();
            ctx.AddRectFilled(Vector2.Zero, io.DisplaySize, Color.Black, 0.0f, eImDrawFlags.None);

            // Get logo
            if (currentLogoTexture == null)
                currentLogoTexture = currentEpisodeResources.GetLoadingTexturesByType(TextureType.Logo).FirstOrDefault().CreateLoadingTexture();

            // Draw loading screen if we are allowed to
            if (canShowLoadingScreen)
            {
                DateTime dtNow = DateTime.UtcNow;
                if (dtNow > addNextLoadingScreenAt && addNextLoadingScreenAt != DateTime.MinValue)
                {
                    AddNextRandomLoadingScreen();
                }

                DrawAndProcessLoadingScreen(ctx);
            }

            // Draw loading screen logo
            DrawAndProcessLoadingScreenLogo(ctx);
        }

    }
}
