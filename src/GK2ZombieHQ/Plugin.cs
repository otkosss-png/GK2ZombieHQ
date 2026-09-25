using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GK2ZombieHQ.Core;
using UnityEngine;

namespace GK2ZombieHQ
{
    [BepInPlugin(Guid, "GK2 Zombie HQ", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "otkosss.gk2.zombiehq";
        public static Plugin Instance;
        public static ManualLogSource Log;

        internal ConfigEntry<bool> HudEnabled;
        internal ConfigEntry<int> HudOffsetX, HudOffsetY, HudFontSize;
        internal ConfigEntry<KeyCode> HudToggleKey, PanelKey;
        internal ConfigEntry<string> Language;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            HudEnabled = Config.Bind("Hud", "Enabled", true, "Show the zombie count HUD");
            HudOffsetX = Config.Bind("Hud", "OffsetX", 12, "HUD X offset (px)");
            HudOffsetY = Config.Bind("Hud", "OffsetY", 12, "HUD Y offset (px)");
            HudFontSize = Config.Bind("Hud", "FontSize", 34, "HUD font size");
            HudToggleKey = Config.Bind("Keys", "HudToggle", KeyCode.F7, "Toggle HUD");
            PanelKey = Config.Bind("Keys", "Panel", KeyCode.F8, "Open the zombie panel");
            Language = Config.Bind("General", "Language", "auto", "auto | en | ru");

            ZombieText.Language = ResolveLanguage(Language.Value);

            var go = new GameObject("GK2ZombieHQ");
            DontDestroyOnLoad(go);
            go.AddComponent<ZombieHud>();
            go.AddComponent<ZombiePanel>();
            Logger.LogInfo("GK2 Zombie HQ " + Version + " loaded.");
        }

        internal static ZombieLanguage ResolveLanguage(string value)
        {
            if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase)) return ZombieLanguage.En;
            if (string.Equals(value, "ru", StringComparison.OrdinalIgnoreCase)) return ZombieLanguage.Ru;
            var two = System.Globalization.CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName;
            return string.Equals(two, "ru", StringComparison.OrdinalIgnoreCase) ? ZombieLanguage.Ru : ZombieLanguage.En;
        }

        internal static string Version => typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}
