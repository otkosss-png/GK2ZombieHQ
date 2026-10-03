using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GK2ZombieHQ.Core;
using UnityEngine;

namespace GK2ZombieHQ
{
    [BepInDependency("ru.superman4eg.gk2.framework")]
    [BepInPlugin(Guid, "GK2 Zombie HQ", "1.5.8")]
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
            ModLocalization.EnsureFiles();

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
            RefreshLanguage(force: true);

            // Патчи по классам: один несошедшийся патч не отключает остальные (как было бы с PatchAll).
            try
            {
                var harmony = new HarmonyLib.Harmony(Guid);
                foreach (var type in typeof(Plugin).Assembly.GetTypes())
                {
                    try { harmony.CreateClassProcessor(type).Patch(); }
                    catch (Exception ex) { Logger.LogWarning("harmony patch " + type.Name + " failed: " + ex.Message); }
                }
            }
            catch (Exception ex) { Logger.LogWarning("harmony patch failed: " + ex.Message); }

            var go = new GameObject("GK2ZombieHQ");
            DontDestroyOnLoad(go);
            go.AddComponent<ZombieHud>();
            go.AddComponent<ZombiePanel>();
            go.AddComponent<ZombieCameraFollow>();
            Logger.LogInfo("GK2 Zombie HQ " + Version + " loaded.");
        }

        // Если Framework недоступен, регистрируем опции сами (тот же .cfg), чтобы мод работал.
        private void EnsureSettings()
        {
            if (Mod.HudEnabled != null) return;
            Mod.Language = Config.Bind("General", "Language", ZombieHqMod.DefaultLanguage, "auto (game language) or a code from the Localization folder: en, ru, de...");
            Mod.HudEnabled = Config.Bind("Hud", "Enabled", true, "Show the zombie count HUD");
            Mod.HudBackground = Config.Bind("Hud", "Background", false, "Draw a dark plate behind the HUD icon and number");
            Mod.HudFontSize = Config.Bind("Hud", "FontSize", 30, "HUD font size");
            Mod.HudOffsetX = Config.Bind("Hud", "OffsetX", 274, "HUD X offset (px)");
            Mod.HudOffsetY = Config.Bind("Hud", "OffsetY", 113, "HUD Y offset (px)");
            Mod.HudToggleKey = Config.Bind("Keys", "HudToggle", new KeyboardShortcut(KeyCode.Z), "Toggle HUD");
            Mod.PanelKey = Config.Bind("Keys", "Panel", new KeyboardShortcut(KeyCode.F8), "Open the zombie panel");
            Mod.PanelGamepad = Config.Bind("Keys", "PanelGamepad", 9, "Gamepad joystick button index to toggle the panel (-1 = off)");
            Mod.CaretakerGuard = Config.Bind("Fix", "CaretakerGuard", true, "Suppress the game's caretaker zombie NullReferenceException");
            Mod.SyncGameCounter = Config.Bind("Fix", "SyncGameCounter", false, "Write the real number of zombies into the game's counter (cur_zombies_count) and recheck the excessive-zombie debuff");
        }

        private static string _languageKey;
        private static float _languageCheckedAt = -100f;

        // Язык надписей. «auto» следует за языком игры — он грузится позже плагина и может
        // смениться в настройках, поэтому перепроверяем раз в пару секунд (из ZombieHud.Update).
        internal static void RefreshLanguage(bool force = false)
        {
            try
            {
                float now = Time.unscaledTime;
                if (!force && now - _languageCheckedAt < 2f) return;
                _languageCheckedAt = now;
                string setting = Mod != null && Mod.Language != null ? Mod.Language.Value : "auto";
                bool auto = string.IsNullOrWhiteSpace(setting) || string.Equals(setting, "auto", StringComparison.OrdinalIgnoreCase);
                string key = setting + "|" + (auto ? ModLocalization.GameLanguage() : "");
                if (key == _languageKey) return;
                _languageKey = key;
                ModLocalization.Apply(setting);
                Log?.LogInfo("language: setting=" + setting + " -> " + ZombieText.Code);
            }
            catch (Exception ex) { Log?.LogWarning("language: " + ex.Message); }
        }

        internal static string Version => typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}
