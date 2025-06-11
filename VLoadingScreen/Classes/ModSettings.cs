using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IVSDKDotNet;

namespace VLoadingScreen.Classes
{
    internal static class ModSettings
    {

        #region Variables
        public static int LoadingScreen_ImageSwitchingInterval;
        public static float LoadingScreen_BackgroundLerpSpeed;
        public static float LoadingScreen_CharacterLerpSpeed;
        public static float LoadingScreen_BackgroundZoomOutSpeed;
        public static float LoadingScreen_CharacterMoveSpeed;

        public static bool Logo_Show;
        public static int Logo_FadingSpeed;
        #endregion

        public static void Load(SettingsFile settings)
        {
            // Load settings
            LoadingScreen_ImageSwitchingInterval = settings.GetInteger("LoadingScreen", "ImageSwitchingInterval", 5500);

            LoadingScreen_BackgroundLerpSpeed = settings.GetFloat("LoadingScreen", "BackgroundLerpSpeed", 0.04f);
            LoadingScreen_CharacterLerpSpeed = settings.GetFloat("LoadingScreen", "CharacterLerpSpeed", 0.04f);

            LoadingScreen_BackgroundZoomOutSpeed = settings.GetFloat("LoadingScreen", "BackgroundZoomOutSpeed", 50.0f);
            LoadingScreen_CharacterMoveSpeed = settings.GetFloat("LoadingScreen", "CharacterMoveSpeed", 20.0f);

            Logo_Show = settings.GetBoolean("Logo", "Show", true);
            Logo_FadingSpeed = settings.GetInteger("Logo", "FadingSpeed", 1);
        }

    }
}
