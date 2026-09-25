using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GK2ZombieHQ.Core;
using UnityEngine;

namespace GK2ZombieHQ
{
    [BepInDependency("ru.superman4eg.gk2.framework")]
    [BepInPlugin(Guid, "GK2 Zombie HQ", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "otkosss.gk2.zombiehq";
        public static Plugin Instance;
        public static ManualLogSource Log;
        internal static ZombieHqMod Mod;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Mod = new ZombieHqMod();
            try
            {
                GK2.Framework.FrameworkApi.RegisterMod(Mod, Config);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("GK2 Framework register failed, using local config: " + ex.Message);
            }
            EnsureSettings();

            ZombieText.Language = ResolveLanguage(Mod.Language.Value);

            var go = new GameObject("GK2ZombieHQ");
            DontDestroyOnLoad(go);
            go.AddComponent<ZombieHud>();
            go.AddComponent<ZombiePanel>();
            Logger.LogInfo("GK2 Zombie HQ " + Version + " loaded.");
        }

        // Если Framework недоступен, регистрируем опции сами (тот же .cfg), чтобы мод работал.
        private void EnsureSettings()
        {
            if (Mod.HudEnabled != null) return;
            Mod.Language = Config.Bind("General", "Language", "auto", "auto | en | ru");
            Mod.HudEnabled = Config.Bind("Hud", "Enabled", true, "Show the zombie count HUD");
            Mod.HudFontSize = Config.Bind("Hud", "FontSize", 40, "HUD font size");
            Mod.HudOffsetX = Config.Bind("Hud", "OffsetX", 520, "HUD X offset (px)");
            Mod.HudOffsetY = Config.Bind("Hud", "OffsetY", 12, "HUD Y offset (px)");
            Mod.HudToggleKey = Config.Bind("Keys", "HudToggle", new KeyboardShortcut(KeyCode.Z), "Toggle HUD");
            Mod.PanelKey = Config.Bind("Keys", "Panel", new KeyboardShortcut(KeyCode.F8), "Open the zombie panel");
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
