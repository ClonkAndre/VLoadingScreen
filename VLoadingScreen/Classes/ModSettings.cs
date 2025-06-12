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

        // General
        public static double SwitchingInterval;
        public static float LerpAmount;

        // Background
        public static float BackgroundDefaultScale;
        public static float PerspectiveChangeSpeedMultiplier;
        public static float ZoomOutAmount;

        // Character
        public static float CharacterDefaultScale;
        public static float CharacterMoveAmount;

        // Logo
        public static bool ShowLogo;
        public static float LogoFadingSpeed;

        #endregion

        public static void Load(SettingsFile settings)
        {
            // General
            SwitchingInterval = settings.GetDouble("General", "SwitchingInterval", 7d);
            LerpAmount =        settings.GetFloat("General", "LerpAmount", 0.06f);

            if (SwitchingInterval < 3d)
                SwitchingInterval = 7.0d;

            // Background
            BackgroundDefaultScale =            settings.GetFloat("Background", "DefaultScale", 0.78f);
            PerspectiveChangeSpeedMultiplier =  settings.GetFloat("Background", "PerspectiveChangeSpeedMultiplier", 2.5f);
            ZoomOutAmount =                     settings.GetFloat("Background", "ZoomOutAmount", 0.00002f);

            // Character
            CharacterDefaultScale = settings.GetFloat("Character", "DefaultScale", 0.85f);
            CharacterMoveAmount =   settings.GetFloat("Character", "MoveAmount", 0.1f);

            // Logo
            ShowLogo =          settings.GetBoolean("Logo", "Show", true);
            LogoFadingSpeed =   settings.GetFloat("Logo", "FadingSpeed", 0.1f);
        }

    }
}
