using System.Collections.Generic;
using System.IO;
using System.Linq;

using IVSDKDotNet;
using IVSDKDotNet.Enums;
using IVSDKDotNet.Native;

namespace VLoadingScreen.Classes
{
    internal class EpisodeResources
    {

        #region Variables
        private bool currentlyCreatingTextures;
        private bool wereTexturesCreated;

        public int EpisodeID;
        public string ResourceFolder;
        public List<TextureResource> Textures;
        #endregion

        #region Constructor
        public EpisodeResources()
        {
            Textures = new List<TextureResource>();
        }
        #endregion

        #region Methods
        public void PreloadTexturesOfType(TextureType type)
        {
            string pathToResources = Path.Combine(Main.Instance.ScriptResourceFolder, ResourceFolder);

            string searchPattern = string.Empty;

            switch (type)
            {
                case TextureType.Background:
                    searchPattern = ".bg.dds";
                    break;
                case TextureType.Character:
                    searchPattern = ".char.dds";
                    break;
                case TextureType.Logo:
                    searchPattern = ".logo.dds";
                    break;
            }

            for (int i = 0; i < Main.Instance.CachedFilesWithinResourcesFolder.Length; i++)
            {
                string path = Main.Instance.CachedFilesWithinResourcesFolder[i];

                // Filter out files which are not within the resources folder of the current episode
                if (!path.StartsWith(pathToResources))
                {
                    //Logging.LogDebug("Path '{0}' does not start with '{1}'!", path, pathToResources);
                    continue;
                }

                // Filter out files which dont match the file extension
                if (!path.EndsWith(searchPattern))
                {
                    //Logging.LogDebug("Path '{0}' does not end with '{1}'!", path, searchPattern);
                    continue;
                }

                string fileName = Path.GetFileName(path);

                if (!File.Exists(path))
                {
                    Logging.LogWarning("File '{0}' is in cache but no longer actually exists on disk!", fileName);
                    continue;
                }

                if (WasTextureCreated(fileName))
                {
                    Logging.LogDebug("Texture {0} was already created. Skipping.", fileName);
                    continue;
                }

                // Try creating texture
                ImGuiIV.CreateTextureFromFile(path, out ImTexture texture, out eResult result);

                if (result != eResult.OK)
                {
                    Logging.LogError("Failed to preload texture from file '{0}'! Details: {1}", fileName, result);
                    continue;
                }

                Logging.LogDebug("Successfully preloaded texture from file '{0}'!", path);

                // Add texture
                Textures.Add(new TextureResource(texture, type, fileName));
            }
        }
        public void LoadSingleTextureOfType(TextureType type)
        {
            string pathToResources = Path.Combine(Main.Instance.ScriptResourceFolder, ResourceFolder);

            string searchPattern = string.Empty;

            switch (type)
            {
                case TextureType.Background:
                    searchPattern = ".bg.dds";
                    break;
                case TextureType.Character:
                    searchPattern = ".char.dds";
                    break;
                case TextureType.Logo:
                    searchPattern = ".logo.dds";
                    break;
            }

            for (int i = 0; i < Main.Instance.CachedFilesWithinResourcesFolder.Length; i++)
            {
                string path = Main.Instance.CachedFilesWithinResourcesFolder[i];

                // Filter out files which are not within the resources folder of the current episode
                if (!path.StartsWith(pathToResources))
                {
                    //Logging.LogDebug("Path '{0}' does not start with '{1}'!", path, pathToResources);
                    continue;
                }

                // Filter out files which dont match the file extension
                if (!path.EndsWith(searchPattern))
                {
                    //Logging.LogDebug("Path '{0}' does not end with '{1}'!", path, searchPattern);
                    continue;
                }

                string fileName = Path.GetFileName(path);

                if (!File.Exists(path))
                {
                    Logging.LogWarning("File '{0}' is in cache but no longer actually exists on disk!", fileName);
                    continue;
                }

                if (WasTextureCreated(fileName))
                {
                    Logging.LogDebug("Texture {0} was already created. Skipping.", fileName);
                    continue;
                }

                // Try creating texture
                ImGuiIV.CreateTextureFromFile(path, out ImTexture texture, out eResult result);

                if (result != eResult.OK)
                {
                    Logging.LogError("Failed to load single texture from file '{0}'! Details: {1}", fileName, result);
                    continue;
                }

                Logging.LogDebug("Successfully loaded single texture from file '{0}'!", path);

                // Add texture
                Textures.Add(new TextureResource(texture, type, fileName));

                break;
            }
        }

        public void ReleaseAllTextures()
        {
            for (int i = 0; i < Textures.Count; i++)
            {
                TextureResource res = Textures[i];
                Logging.LogDebug("Releasing texture {0}. Result: {1}", res.FileName, res.Release());
            }
            Textures.Clear();

            wereTexturesCreated = false;
        }
        #endregion

        #region Functions
        public bool CreateAllTextures()
        {
            if (wereTexturesCreated)
                return true;
            if (currentlyCreatingTextures)
                return false;

            currentlyCreatingTextures = true;

            string pathToResources = Path.Combine(Main.Instance.ScriptResourceFolder, ResourceFolder);

            for (int i = 0; i < Main.Instance.CachedFilesWithinResourcesFolder.Length; i++)
            {
                string path = Main.Instance.CachedFilesWithinResourcesFolder[i];
                string fileName = Path.GetFileName(path);

                // Filter out files which are not within the resources folder of the current episode
                if (!path.StartsWith(pathToResources))
                {
                    continue;
                }

                if (!File.Exists(path))
                {
                    Logging.LogWarning("File '{0}' is in cache but no longer actually exists on disk!", fileName);
                    continue;
                }

                if (WasTextureCreated(fileName))
                {
                    Logging.LogDebug("Texture {0} was already created. Skipping.", fileName);
                    continue;
                }

                // Determine image type
                TextureType imageType = DetermineTextureTypeFromFileName(Path.GetFileNameWithoutExtension(path));

                if (imageType == TextureType.Unknown)
                    continue;

                // Try creating texture
                ImGuiIV.CreateTextureFromFile(path, out ImTexture texture, out eResult result);

                if (result != eResult.OK)
                {
                    Logging.LogError("Failed to create texture from file '{0}'! Details: {1}", fileName, result);
                    continue;
                }

                Logging.LogDebug("Successfully created texture from file '{0}'!", path);

                // Add texture
                Textures.Add(new TextureResource(texture, imageType, fileName));
            }

            currentlyCreatingTextures = false;
            wereTexturesCreated = true;
            return true;
        }

        public bool WereAllTexturesCreated() => wereTexturesCreated;
        public bool WasTextureCreated(string fileName)
        {
            return Textures.Any(x => x.FileName == fileName);
        }

        public IEnumerable<TextureResource> GetLoadingTexturesByType(TextureType type)
        {
            return Textures.Where(x => x.Type == type);
        }
        public TextureResource GetRandomLoadingTextureByType(TextureType type)
        {
            TextureResource[] textures = GetLoadingTexturesByType(type).ToArray();

            if (textures.Length == 0)
                return null;

            return textures[Natives.GENERATE_RANDOM_INT_IN_RANGE(0, textures.Length)];
        }

        private TextureType DetermineTextureTypeFromFileName(string fileName)
        {
            if (fileName.EndsWith(".bg"))
                return TextureType.Background;
            else if (fileName.EndsWith(".char"))
                return TextureType.Character;
            else if (fileName.EndsWith(".logo"))
                return TextureType.Logo;

            return TextureType.Unknown;
        }
        #endregion

    }
}
