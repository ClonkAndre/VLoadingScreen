using System.Numerics;

using IVSDKDotNet;

namespace VLoadingScreen.Classes
{
    internal class LoadingScreen
    {

        #region Variables
        public StartingPosition InitialStartingPos;
        public TargetPosition TargetPos;
        public bool ReachedTargetPosition;

        public LoadingTexture BackgroundTexture;
        public LoadingTexture CharacterTexture;
        #endregion

        public LoadingScreen(StartingPosition startPos, LoadingTexture backgroundTexture, LoadingTexture characterTexture)
        {
            InitialStartingPos = startPos;
            TargetPos = TargetPosition.Center;

            BackgroundTexture = backgroundTexture;
            CharacterTexture = characterTexture;

            ImGuiIV_IO io = ImGuiIV.GetIO();

            switch (startPos)
            {
                case StartingPosition.Left:
                    BackgroundTexture.Position =    new Vector2(0f - BackgroundTexture.GetSize().X, io.DisplaySize.Y * 0.5f); // Background
                    CharacterTexture.Position =     new Vector2(0f - CharacterTexture.GetSize().X, io.DisplaySize.Y); // Character
                    break;
                case StartingPosition.Right:
                    BackgroundTexture.Position =    new Vector2(io.DisplaySize.X + BackgroundTexture.GetSize().X, io.DisplaySize.Y * 0.5f); // Background
                    CharacterTexture.Position =     new Vector2(io.DisplaySize.X + CharacterTexture.GetSize().X, io.DisplaySize.Y); // Character
                    break;
            }
        }

    }
}
