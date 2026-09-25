using BepInEx.Configuration;
using GK2.Framework;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Регистрация мода и настроек в GK2 Mod Framework: опции появляются в игровом меню Mods.
    internal sealed class ZombieHqMod : Gk2ModBase
    {
        private readonly Gk2ModMetadata _metadata = new Gk2ModMetadata(
            "otkosss.gk2.zombiehq",
            "GK2 Zombie HQ",
            "otkosss",
            "1.0.0",
            "Zombie count HUD and a manager panel: list every zombie, open the game's zombie window, recall from work.",
            false,
            false);

        internal ConfigEntry<string> Language;
        internal ConfigEntry<bool> HudEnabled;
        internal ConfigEntry<int> HudFontSize, HudOffsetX, HudOffsetY, HudMaxZombies;
        internal ConfigEntry<KeyboardShortcut> HudToggleKey, PanelKey;

        public override Gk2ModMetadata Metadata => _metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            var s = context.Settings;
            Language = s.AddDropdown("General", "Language", "en", new[] { "auto", "en", "ru" },
                "Language / Язык", "en, ru, auto (system)", 10);
            HudEnabled = s.AddToggle("Hud", "Enabled", true,
                "HUD: счётчик зомби", "Показывать счётчик зомби на экране", 10);
            HudFontSize = s.AddIntSlider("Hud", "FontSize", 60, 12, 160,
                "Размер шрифта HUD", "", 2, 20);
            HudOffsetX = s.AddIntSlider("Hud", "OffsetX", 12, 0, 1900,
                "Отступ HUD по X", "", 2, 30);
            HudOffsetY = s.AddIntSlider("Hud", "OffsetY", 1000, 0, 1080,
                "Отступ HUD по Y", "", 2, 40);
            HudMaxZombies = s.AddIntSlider("Hud", "MaxZombies", 0, 0, 300,
                "Allowed zombies (0 = hide)", "Shown as N / L; N turns red when above L", 1, 50);
            HudToggleKey = s.AddKeybind("Keys", "HudToggle", new KeyboardShortcut(KeyCode.Z),
                "Клавиша HUD", "Скрыть/показать счётчик", 10);
            PanelKey = s.AddKeybind("Keys", "Panel", new KeyboardShortcut(KeyCode.F8),
                "Клавиша панели", "Открыть «Зомби-штаб»", 20);
        }
    }
}
