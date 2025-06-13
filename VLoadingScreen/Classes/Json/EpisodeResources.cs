using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Newtonsoft.Json;

using IVSDKDotNet;
using IVSDKDotNet.Enums;
using IVSDKDotNet.Native;

namespace VLoadingScreen.Classes.Json
{
    internal class EpisodeResources
    {

        #region Variables
        public int EpisodeID;

        public string ResourceFolder;
        private string pathToResourceFolder;
        public string[] ResourceFolderContent;

        public Vector2 CharacterScale;

        public List<TextureResource> Textures;
        #endregion

        #region Constructor
        public EpisodeResources()
        {
            Textures = new List<TextureResource>();
        }
        #endregion

        #region Methods
        public void Init()
        {
            // Construct path to folder
            pathToResourceFolder = Path.Combine(Main.Instance.ScriptResourceFolder, ResourceFolder);
            
            // Get all files within this resource folder
            ResourceFolderContent = Main.Instance.CachedFilesWithinResourcesFolder.Where(x => x.StartsWith(pathToResourceFolder)).ToArray();
        }

        public void CreateAllTextures()
        {
            for (int i = 0; i < ResourceFolderContent.Length; i++)
            {
                string path = ResourceFolderContent[i];
                string fileName = Path.GetFileName(path);

                // Check if file still exists, as it is no guranteed to
                if (!File.Exists(path))
                {
                    Logging.LogWarning("File '{0}' is in cache but no longer actually exists on disk!", fileName);
                    continue;
                }

                // Check if this file is even a texture
                if (!path.EndsWith(".dds"))
                    continue;

                // Check if this texture was already created
                if (WasTextureCreated(fileName))
                {
                    Logging.LogDebug("Texture '{0}' was already created. Skipping.", fileName);
                    continue;
                }

                // Determine the file type
                TextureType fileType = DetermineTextureTypeFromFileName(Path.GetFileNameWithoutExtension(path));

                if (fileType == TextureType.Unknown)
                    continue;

                ImGuiIV.CreateTextureFromFile(path, out ImTexture texture, out eResult result);

                if (result != eResult.OK)
                {
                    Logging.LogError("Failed to create texture from file '{0}'! Details: {1}", fileName, result);
                    continue;
                }

                Logging.LogDebug("Successfully created texture from file '{0}'!", path);

                // Add texture
                Textures.Add(new TextureResource(texture, fileType, fileName));
            }
        }
        public void CreateTexturesOfType(TextureType type)
        {
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

            for (int i = 0; i < ResourceFolderContent.Length; i++)
            {
                string path = ResourceFolderContent[i];

                // Filter out files which dont match the file extension
                if (!path.EndsWith(searchPattern))
                {
                    //Logging.LogDebug("Path '{0}' does not end with '{1}'!", path, searchPattern);
                    continue;
                }

                string fileName = Path.GetFileName(path);

                // Check if file still exists, as it is no guranteed to
                if (!File.Exists(path))
                {
                    Logging.LogWarning("File '{0}' is in cache but no longer actually exists on disk!", fileName);
                    continue;
                }

                // Check if this texture was already created
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
        public void ReadAllTextureConfigFiles()
        {
            for (int i = 0; i < ResourceFolderContent.Length; i++)
            {
                string path = ResourceFolderContent[i];
                string fileName = Path.GetFileName(path);

                try
                {
                    // Check if file still exists, as it is no guranteed to
                    if (!File.Exists(path))
                    {
                        Logging.LogWarning("File '{0}' is in cache but no longer actually exists on disk!", fileName);
                        continue;
                    }

                    // Check if this file is even a texture config
                    if (!path.EndsWith(".json"))
                        continue;

                    // Determine the file type
                    TextureType fileType = DetermineTextureTypeFromFileName(Path.GetFileNameWithoutExtension(path));

                    if (fileType == TextureType.Unknown
                        || fileType != TextureType.Background)
                        continue;

                    // Try get the texture this config file belongs to
                    TextureResource foundResource = GetLoadingTextureByFileName(fileName.Replace(".json", ".dds"));

                    if (foundResource == null)
                        continue;
                    if (foundResource.BackgroundTextureConfig != null)
                        continue;

                    // Read file content
                    string content = File.ReadAllText(path);

                    // Remove all comments
                    content = string.Join(Environment.NewLine,
                        content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Where(line => !line.TrimStart().StartsWith("//")));

                    // Load and assign config to found resource
                    foundResource.BackgroundTextureConfig = JsonConvert.DeserializeObject<BackgroundTextureConfig>(content);

                    Logging.LogDebug("Loaded config file for background image '{0}'", fileName);
                }
                catch (Exception ex)
                {
                    Logging.LogError("Failed to load texture config file '{0}'! Details: {1}", fileName, ex);
                }
            }
        }

        public void ReleaseAllTextures(TextureType ofType = TextureType.All)
        {
            for (int i = 0; i < Textures.Count; i++)
            {
                TextureResource res = Textures[i];

                if (ofType == TextureType.All)
                {
                    Logging.LogDebug("Releasing texture {0}. Result: {1}", res.FileName, res.Release());
                    Textures.RemoveAt(i);
                    i--;
                }
                else
                {
                    if (ofType == res.Type)
                    {
                        Logging.LogDebug("Releasing texture {0} because its of type {1}. Result: {2}", res.FileName, ofType, res.Release());
                        Textures.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        #endregion

        #region Functions
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
        public TextureResource GetLoadingTextureByFileName(string fileName)
        {
            return Textures.Where(x => x.FileName == fileName).FirstOrDefault();
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
