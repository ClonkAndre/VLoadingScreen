using System.Numerics;

using IVSDKDotNet;
using IVSDKDotNet.Native;

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

        #region Constructor
        public LoadingScreen(StartingPosition forcedStartingPos, LoadingTexture backgroundTexture, LoadingTexture characterTexture)
        {
            InitialStartingPos = forcedStartingPos;
            TargetPos = TargetPosition.Center;

            BackgroundTexture = backgroundTexture;
            CharacterTexture = characterTexture;

            Init();
        }
        public LoadingScreen(LoadingTexture backgroundTexture, LoadingTexture characterTexture)
        {
            InitialStartingPos = (StartingPosition)Natives.GENERATE_RANDOM_INT_IN_RANGE(0, 2);
            TargetPos = TargetPosition.Center;

            BackgroundTexture = backgroundTexture;
            CharacterTexture = characterTexture;

            Init();
        }
        #endregion

        private void Init()
        {
            ImGuiIV_IO io = ImGuiIV.GetIO();

            // Set the position for each texture based on the initial starting position
            switch (InitialStartingPos)
            {
                case StartingPosition.Left:
                    BackgroundTexture.Position = new Vector2(0f - BackgroundTexture.GetSize().X, io.DisplaySize.Y * 0.5f); // Background
                    CharacterTexture.Position = new Vector2(0f - CharacterTexture.GetSize().X, io.DisplaySize.Y); // Character
                    break;
                case StartingPosition.Right:
                    BackgroundTexture.Position = new Vector2(io.DisplaySize.X + BackgroundTexture.GetSize().X, io.DisplaySize.Y * 0.5f); // Background
                    CharacterTexture.Position = new Vector2(io.DisplaySize.X + CharacterTexture.GetSize().X, io.DisplaySize.Y); // Character
                    break;
            }
        }

    }
}
