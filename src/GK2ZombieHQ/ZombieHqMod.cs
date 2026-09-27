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
            "1.4.2",
            "Zombie count HUD and a manager panel: list every zombie (including ones lying on the floor), show the total in the save, open the game's zombie window, recall from work, and guard against the game's caretaker-zombie crash.",
            false,
            false);

        internal ConfigEntry<string> Language;
        internal ConfigEntry<bool> HudEnabled;
        internal ConfigEntry<bool> HudBackground;
        internal ConfigEntry<bool> CaretakerGuard;
        internal ConfigEntry<int> HudFontSize, HudOffsetX, HudOffsetY;
        internal ConfigEntry<KeyboardShortcut> HudToggleKey, PanelKey;
        internal ConfigEntry<int> PanelGamepad;

        public override Gk2ModMetadata Metadata => _metadata;

        public override void OnRegister(Gk2ModContext context)
        {
            var s = context.Settings;
            Language = s.AddDropdown("General", "Language", "en", new[] { "auto", "en", "ru" },
                "Language / Язык", "en, ru, auto (system)", 10);
            HudEnabled = s.AddToggle("Hud", "Enabled", true,
                "HUD: счётчик зомби", "Показывать счётчик зомби на экране", 10);
            HudBackground = s.AddToggle("Hud", "Background", false,
                "Фон HUD", "Плашка под иконкой и числом (выкл — только иконка и число)", 11);
            HudFontSize = s.AddIntSlider("Hud", "FontSize", 30, 12, 160,
                "Размер шрифта HUD", "", 2, 20);
            HudOffsetX = s.AddIntSlider("Hud", "OffsetX", 274, 0, 1900,
                "Отступ HUD по X", "", 2, 30);
            HudOffsetY = s.AddIntSlider("Hud", "OffsetY", 113, 0, 1080,
                "Отступ HUD по Y", "", 2, 40);
            HudToggleKey = s.AddKeybind("Keys", "HudToggle", new KeyboardShortcut(KeyCode.Z),
                "Клавиша HUD", "Скрыть/показать счётчик", 10);
            PanelKey = s.AddKeybind("Keys", "Panel", new KeyboardShortcut(KeyCode.F8),
                "Клавиша панели", "Открыть «Зомби-штаб»", 20);
            PanelGamepad = s.AddIntSlider("Keys", "PanelGamepad", 6, -1, 19,
                "Gamepad: open panel (button #)", "Joystick button index (0=A,1=B,2=X,3=Y,6=Back,7=Start); -1 = off", 1, 30);
            CaretakerGuard = s.AddToggle("Fix", "CaretakerGuard", true,
                "Защита от ошибок смотрителя", "Глушить игровой NRE у смотрителя с пропавшей целью (иначе игра падает каждый кадр)", 10);
        }
    }
}
