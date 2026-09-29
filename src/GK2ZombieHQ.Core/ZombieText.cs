using System;
using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    // Все надписи мода. Встроены en и ru; любой язык (и правки en/ru) приходят из
    // Localization\<код>.json рядом с DLL (см. ModLocalization). Порядок поиска строки:
    // файл перевода → встроенный язык → английский. Пустая строка в файле = «не переведено».
    public static class ZombieText
    {
        private static readonly Dictionary<string, (string En, string Ru)> Table =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["Title"] = ("Zombie HQ", "Зомби-штаб"),
            ["Zombies"] = ("Zombies", "Зомби"),
            ["Open"] = ("Open", "Открыть"),
            ["Recall"] = ("Recall", "Отозвать"),
            ["Camera"] = ("Camera", "Камера"),
            ["CameraFollow"] = ("Camera follows", "Камера следит за"),
            ["EscExit"] = ("Esc - exit", "Esc - выйти"),
            ["Center"] = ("Center", "Центр"),
            ["Close"] = ("Close", "Закрыть"),
            ["NoZombies"] = ("No zombies", "Зомби нет"),
            ["Free"] = ("free", "свободен"),
            ["Lying"] = ("on the floor", "лежит на полу"),
            ["NoStation"] = ("no station", "без станции"),
            ["InHands"] = ("in hands", "в руках"),
            ["OnTable"] = ("on a pallet/table", "на паллете/столе"),
            ["InChoir"] = ("in the church choir", "в хоре"),
            ["OffWorld"] = ("no body in the world", "нет тела в мире"),
            ["Error"] = ("Error", "Ошибка"),
            ["Collar"] = ("Collar", "Ошейник"),
            ["Tool"] = ("Tool", "Инструмент"),
            ["Armor"] = ("Armor", "Броня"),
            ["Empty"] = ("empty", "пусто"),
            ["Carried"] = ("Carried", "Переносимое"),
            ["kind.Unknown"] = ("Unknown", "Неизвестно"),
            ["kind.Free"] = ("Free", "Свободный"),
            ["kind.Crafter"] = ("Crafter", "Ремесленник"),
            ["kind.Caretaker"] = ("Caretaker", "Смотритель"),
            ["kind.ConveyorCrafter"] = ("Conveyor crafter", "Конвейерщик"),
            ["kind.Worker"] = ("Worker", "Рабочий"),
            ["kind.Porter"] = ("Porter", "Носильщик"),
            ["kind.Gardener"] = ("Gardener", "Садовник"),
            ["kind.ConveyorTransporter"] = ("Transporter", "Транспортёр"),
            ["kind.Fighter"] = ("Fighter", "Боец"),
            ["kind.Wheel"] = ("On the wheel", "На колесе"),
        };

        private static Dictionary<string, string> _overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool _ru;

        // Текущий язык (код файла: en, ru, de …).
        public static string Code { get; private set; } = "en";

        public static IEnumerable<string> Keys => Table.Keys;

        // Выбрать язык: встроенный ru для «ru», иначе английский; overrides — строки из файла перевода.
        public static void Use(string code, IDictionary<string, string> overrides = null)
        {
            Code = string.IsNullOrWhiteSpace(code) ? "en" : code.Trim().ToLowerInvariant();
            _ru = Code == "ru";
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (overrides != null)
                foreach (var pair in overrides)
                    if (!string.IsNullOrEmpty(pair.Key) && !string.IsNullOrEmpty(pair.Value))
                        map[pair.Key.Trim()] = pair.Value;
            _overrides = map;
        }

        // Шаблон файла перевода: все ключи со встроенными строками языка.
        public static Dictionary<string, string> Template(string code)
        {
            bool ru = string.Equals(code, "ru", StringComparison.OrdinalIgnoreCase);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in Table) result[pair.Key] = ru ? pair.Value.Ru : pair.Value.En;
            return result;
        }

        public static string Get(string key)
        {
            if (key == null || !Table.TryGetValue(key, out var v))
                throw new KeyNotFoundException("ZombieText key not found: " + (key ?? "<null>"));
            if (_overrides.TryGetValue(key, out var custom)) return custom;
            return _ru ? v.Ru : v.En;
        }

        public static string KindName(ZombieKind kind)
        {
            var key = "kind." + kind;
            return Table.ContainsKey(key) ? Get(key) : Get("kind.Unknown");
        }

        // Подпись состояния строки: "лежит на полу" / "без станции" / "в руках" / "нет тела в мире"
        // (у работающих подписи нет).
        public static string StatusName(ZombieState state)
        {
            switch (state)
            {
                case ZombieState.Lying: return Get("Lying");
                case ZombieState.Free: return Get("NoStation");
                case ZombieState.InHands: return Get("InHands");
                case ZombieState.OnTable: return Get("OnTable");
                case ZombieState.InChoir: return Get("InChoir");
                case ZombieState.OffWorld: return Get("OffWorld");
                default: return null;
            }
        }

        // Название слота снаряжения ("Ошейник" / "Инструмент" / "Броня").
        public static string SlotName(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Collar: return Get("Collar");
                case GearSlot.Tool: return Get("Tool");
                case GearSlot.Armor: return Get("Armor");
                default: return Get("Carried");
            }
        }

        // Подсказка значка: "Ошейник: Кожаный ошейник", "Переносимое: Шприц x3".
        public static string GearTip(GearIcon icon)
        {
            if (icon == null) return string.Empty;
            string slot = SlotName(icon.Slot);
            if (icon.IsEmpty) return slot + ": " + Get("Empty");
            string name = string.IsNullOrEmpty(icon.Name) ? icon.Id : icon.Name;
            string count = icon.Count > 1 ? " x" + icon.Count : string.Empty;
            return slot + ": " + name + count;
        }
    }
}
