using System.Collections.Generic;
using BepInEx.Configuration;
using GK2.Framework;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Настройки мода БЕЗ типов GK2 Framework: весь остальной код читает их отсюда всегда,
    // даже когда Framework не установлен (мягкая зависимость).
    internal sealed class ZombieHqMod
    {
        internal const string DefaultLanguage = "auto";

        internal ConfigEntry<string> Language;
        internal ConfigEntry<bool> HudEnabled;
        internal ConfigEntry<bool> HudBackground;
        internal ConfigEntry<bool> CaretakerGuard;
        internal ConfigEntry<bool> SyncGameCounter;
        internal ConfigEntry<int> HudFontSize, HudOffsetX, HudOffsetY;
        internal ConfigEntry<KeyboardShortcut> HudToggleKey, PanelKey;
        internal ConfigEntry<int> PanelGamepad;
    }

    // Мост к игровому меню Mods (GK2 Mod Framework). Класс трогается ТОЛЬКО из
    // Plugin.RegisterWithFramework, когда Framework найден: иначе загрузка типа упала бы.
    // Подписи здесь — английские заглушки; переводы берутся из Localization\<код>.json (ModLocalization).
    internal sealed class FrameworkMod : Gk2ModBase
    {
        private readonly Gk2ModMetadata _metadata = new Gk2ModMetadata(
            "otkosss.gk2.zombiehq",
            "GK2 Zombie HQ",
            "otkosss",
            "1.5.10",
            "Zombie count HUD and a manager panel: list every zombie (including ones lying on the floor), show the total in the save, open the game's zombie window, recall from work, and guard against the game's caretaker-zombie crash.",
            false,
            false);

        public override Gk2ModMetadata Metadata => _metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            var s = context.Settings;
            var m = Plugin.Mod;
            // Языки: auto + встроенные en/ru + все Localization\<код>.json рядом с модом.
            var languages = new List<string> { "auto" };
            languages.AddRange(ModLocalization.AvailableCodes());
            m.Language = s.AddDropdown("General", "Language", ZombieHqMod.DefaultLanguage, languages.ToArray(),
                "Language", "auto = game language; or a code from the Localization folder (en, ru, de...)", 10);
            m.HudEnabled = s.AddToggle("Hud", "Enabled", true,
                "HUD: zombie counter", "Show the zombie counter on screen", 10);
            m.HudBackground = s.AddToggle("Hud", "Background", false,
                "HUD background", "Dark plate under the icon and number (off - icon and number only)", 11);
            m.HudFontSize = s.AddIntSlider("Hud", "FontSize", 30, 12, 160,
                "HUD font size", "", 2, 20);
            m.HudOffsetX = s.AddIntSlider("Hud", "OffsetX", 274, 0, 1900,
                "HUD offset X", "", 2, 30);
            m.HudOffsetY = s.AddIntSlider("Hud", "OffsetY", 113, 0, 1080,
                "HUD offset Y", "", 2, 40);
            m.HudToggleKey = s.AddKeybind("Keys", "HudToggle", new KeyboardShortcut(KeyCode.Z),
                "HUD key", "Hide/show the counter", 10);
            m.PanelKey = s.AddKeybind("Keys", "Panel", new KeyboardShortcut(KeyCode.F8),
                "Panel key", "Open Zombie HQ", 20);
            m.PanelGamepad = s.AddIntSlider("Keys", "PanelGamepad", 9, -1, 19,
                "Gamepad: open panel (button #)", "Joystick button index (0=A,1=B,2=X,3=Y,6=Back,7=Start); -1 = off", 1, 30);
            m.CaretakerGuard = s.AddToggle("Fix", "CaretakerGuard", true,
                "Caretaker crash guard", "Suppress the game's caretaker NullReferenceException (otherwise it fires every frame)", 10);
            m.SyncGameCounter = s.AddToggle("Fix", "SyncGameCounter", false,
                "Sync the game's zombie counter", "Write the real number of zombies into the game's counter and recheck the 'too many zombies' debuff (for zombies removed by other mods)", 20);
        }
    }
}
