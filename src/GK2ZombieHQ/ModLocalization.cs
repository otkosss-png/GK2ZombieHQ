using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GK2ZombieHQ.Core;
using Newtonsoft.Json;

namespace GK2ZombieHQ
{
    // Переводы мода: BepInEx\plugins\<папка мода>\Localization\<код>.json (en.json, ru.json, de.json …).
    // В одном файле и надписи HUD/панели (ключи ZombieText), и строки экрана настроек
    // (mod.*, settings.*). Строки настроек копируются туда, где их ищет GK2 Mod Framework:
    // BepInEx\plugins\GK2.Framework\Localization\<modId>\<код>.json.
    // en.json/ru.json создаются, если их нет, — это шаблон для переводчиков; правки игрока не
    // перезаписываются. Язык «auto» = язык игры.
    internal static class ModLocalization
    {
        internal const string ModId = "otkosss.gk2.zombiehq";

        private static readonly Dictionary<string, string> SettingsEn = new Dictionary<string, string>
        {
            { "mod.name", "GK2 Zombie HQ" },
            { "mod.description", "Zombie count HUD and a manager panel: every zombie with skulls, gear and status; open, camera, recall." },
            { "settings.General.Language.name", "Language" },
            { "settings.General.Language.description", "auto = game language; or a code from the Localization folder (en, ru, de...)" },
            { "settings.Hud.Enabled.name", "HUD: zombie counter" },
            { "settings.Hud.Enabled.description", "Show the zombie counter on screen" },
            { "settings.Hud.Background.name", "HUD background" },
            { "settings.Hud.Background.description", "Dark plate under the icon and number (off - icon and number only)" },
            { "settings.Hud.FontSize.name", "HUD font size" },
            { "settings.Hud.FontSize.description", "" },
            { "settings.Hud.OffsetX.name", "HUD offset X" },
            { "settings.Hud.OffsetX.description", "" },
            { "settings.Hud.OffsetY.name", "HUD offset Y" },
            { "settings.Hud.OffsetY.description", "" },
            { "settings.Keys.HudToggle.name", "HUD key" },
            { "settings.Keys.HudToggle.description", "Hide/show the counter" },
            { "settings.Keys.Panel.name", "Panel key" },
            { "settings.Keys.Panel.description", "Open Zombie HQ" },
            { "settings.Keys.PanelGamepad.name", "Gamepad: open panel (button #)" },
            { "settings.Keys.PanelGamepad.description", "Joystick button index (0=A,1=B,2=X,3=Y,6=Back,7=Start); -1 = off" },
            { "settings.Fix.CaretakerGuard.name", "Caretaker crash guard" },
            { "settings.Fix.CaretakerGuard.description", "Suppress the game's caretaker NullReferenceException (otherwise it fires every frame)" },
            { "settings.Fix.SyncGameCounter.name", "Sync the game's zombie counter" },
            { "settings.Fix.SyncGameCounter.description", "Write the real number of zombies into the game's counter and recheck the 'too many zombies' debuff (for zombies removed by other mods)" },
        };

        private static readonly Dictionary<string, string> SettingsRu = new Dictionary<string, string>
        {
            { "mod.name", "GK2 Зомби-штаб" },
            { "mod.description", "Счётчик зомби на экране и панель: все зомби с черепами, снаряжением и статусом; открыть, камера, отозвать." },
            { "settings.General.Language.name", "Язык" },
            { "settings.General.Language.description", "auto = язык игры; или код файла из папки Localization (en, ru, de…)" },
            { "settings.Hud.Enabled.name", "HUD: счётчик зомби" },
            { "settings.Hud.Enabled.description", "Показывать счётчик зомби на экране" },
            { "settings.Hud.Background.name", "Фон HUD" },
            { "settings.Hud.Background.description", "Плашка под иконкой и числом (выкл — только иконка и число)" },
            { "settings.Hud.FontSize.name", "Размер шрифта HUD" },
            { "settings.Hud.FontSize.description", "" },
            { "settings.Hud.OffsetX.name", "Отступ HUD по X" },
            { "settings.Hud.OffsetX.description", "" },
            { "settings.Hud.OffsetY.name", "Отступ HUD по Y" },
            { "settings.Hud.OffsetY.description", "" },
            { "settings.Keys.HudToggle.name", "Клавиша HUD" },
            { "settings.Keys.HudToggle.description", "Скрыть/показать счётчик" },
            { "settings.Keys.Panel.name", "Клавиша панели" },
            { "settings.Keys.Panel.description", "Открыть «Зомби-штаб»" },
            { "settings.Keys.PanelGamepad.name", "Геймпад: открыть панель (кнопка №)" },
            { "settings.Keys.PanelGamepad.description", "Номер кнопки джойстика (0=A,1=B,2=X,3=Y,6=Back,7=Start); -1 = выкл" },
            { "settings.Fix.CaretakerGuard.name", "Защита от ошибок смотрителя" },
            { "settings.Fix.CaretakerGuard.description", "Глушить игровой NRE у смотрителя с пропавшей целью (иначе игра падает каждый кадр)" },
            { "settings.Fix.SyncGameCounter.name", "Синхронизировать счётчик игры" },
            { "settings.Fix.SyncGameCounter.description", "Записывать в счётчик игры реальное число зомби и пересчитывать штраф «слишком много зомби» (если зомби убрал другой мод)" },
        };

        internal static string Dir
        {
            get
            {
                var self = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(string.IsNullOrEmpty(self) ? BepInEx.Paths.PluginPath : self, "Localization");
            }
        }

        // Создать шаблоны en/ru (если их нет) и разложить строки настроек для Framework.
        internal static void EnsureFiles()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                WriteTemplateIfMissing("en", SettingsEn);
                WriteTemplateIfMissing("ru", SettingsRu);
                SyncFrameworkSettings();
            }
            catch (Exception ex) { Plugin.Log?.LogWarning("localization: " + ex.Message); }
        }

        // Коды языков, для которых есть файл (для списка в настройках).
        internal static List<string> AvailableCodes()
        {
            var codes = new List<string>();
            try
            {
                if (Directory.Exists(Dir))
                    foreach (var f in Directory.GetFiles(Dir, "*.json"))
                    {
                        var code = Path.GetFileNameWithoutExtension(f).Trim().ToLowerInvariant();
                        if (IsValidCode(code) && !codes.Contains(code)) codes.Add(code);
                    }
            }
            catch { }
            foreach (var builtin in new[] { "ru", "en" })
                if (!codes.Contains(builtin)) codes.Add(builtin);
            codes.Sort(StringComparer.Ordinal);
            return codes;
        }

        // setting: auto | код. Для auto — язык игры; zh_cn → zh_cn, затем zh; нет файла → встроенный.
        internal static void Apply(string setting)
        {
            string wanted = string.IsNullOrWhiteSpace(setting) || string.Equals(setting, "auto", StringComparison.OrdinalIgnoreCase)
                ? GameLanguage()
                : setting.Trim().ToLowerInvariant().Replace('-', '_');

            foreach (var code in Candidates(wanted))
            {
                var file = ReadFile(code);
                if (file != null) { ZombieText.Use(code, file); return; }
                if (code == "en" || code == "ru") { ZombieText.Use(code); return; }
            }
            ZombieText.Use("en");
        }

        internal static string GameLanguage()
        {
            try { return GK2.Framework.FrameworkLocalization.CurrentLanguage ?? "en"; }
            catch { return "en"; }
        }

        private static IEnumerable<string> Candidates(string code)
        {
            if (!IsValidCode(code)) { yield return "en"; yield break; }
            yield return code;
            int sep = code.IndexOf('_');
            if (sep > 0) yield return code.Substring(0, sep);
            yield return "en";
        }

        private static bool IsValidCode(string code)
            => !string.IsNullOrEmpty(code) && code.Length <= 16
               && code.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_');

        private static Dictionary<string, string> ReadFile(string code)
        {
            var path = Path.Combine(Dir, code + ".json");
            if (!File.Exists(path)) return null;
            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8));
                return parsed ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning("localization: cannot read " + code + ".json: " + ex.Message);
                return null;
            }
        }

        private static void WriteTemplateIfMissing(string code, Dictionary<string, string> settings)
        {
            var path = Path.Combine(Dir, code + ".json");
            if (File.Exists(path)) return;
            var all = new Dictionary<string, string>();
            foreach (var pair in ZombieText.Template(code)) all[pair.Key] = pair.Value;
            foreach (var pair in settings) all[pair.Key] = pair.Value;
            File.WriteAllText(path, JsonConvert.SerializeObject(all, Formatting.Indented), new UTF8Encoding(false));
        }

        // Строки mod.*/settings.* из каждого нашего файла — в папку переводов GK2 Mod Framework.
        private static void SyncFrameworkSettings()
        {
            var target = Path.Combine(BepInEx.Paths.PluginPath, "GK2.Framework", "Localization", ModId);
            Directory.CreateDirectory(target);
            foreach (var code in AvailableCodes())
            {
                var file = ReadFile(code) ?? new Dictionary<string, string>();
                var fallback = code == "ru" ? SettingsRu : code == "en" ? SettingsEn : null;
                var settings = new Dictionary<string, string>();
                if (fallback != null) foreach (var pair in fallback) settings[pair.Key] = pair.Value;
                foreach (var pair in file)
                    if (!string.IsNullOrEmpty(pair.Value) && (pair.Key.StartsWith("settings.", StringComparison.Ordinal)
                        || pair.Key.StartsWith("mod.", StringComparison.Ordinal)))
                        settings[pair.Key] = pair.Value;
                if (settings.Count == 0) continue;
                WriteIfChanged(Path.Combine(target, code + ".json"), JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            try { GK2.Framework.FrameworkLocalization.Reload(ModId); } catch { }
        }

        private static void WriteIfChanged(string path, string content)
        {
            if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == content) return;
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
}
