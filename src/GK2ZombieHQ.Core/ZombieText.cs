using System;
using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public static class ZombieText
    {
        public static ZombieLanguage Language { get; set; } = ZombieLanguage.En;

        private static readonly Dictionary<string, (string En, string Ru)> Table =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["Title"] = ("Zombie HQ", "Зомби-штаб"),
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
        };

        private static readonly Dictionary<ZombieKind, (string En, string Ru)> Kinds =
            new Dictionary<ZombieKind, (string, string)>
        {
            [ZombieKind.Unknown] = ("Unknown", "Неизвестно"),
            [ZombieKind.Free] = ("Free", "Свободный"),
            [ZombieKind.Crafter] = ("Crafter", "Ремесленник"),
            [ZombieKind.Caretaker] = ("Caretaker", "Смотритель"),
            [ZombieKind.ConveyorCrafter] = ("Conveyor crafter", "Конвейерщик"),
            [ZombieKind.Worker] = ("Worker", "Рабочий"),
            [ZombieKind.Porter] = ("Porter", "Носильщик"),
            [ZombieKind.Gardener] = ("Gardener", "Садовник"),
            [ZombieKind.ConveyorTransporter] = ("Transporter", "Транспортёр"),
            [ZombieKind.Fighter] = ("Fighter", "Боец"),
        };

        public static string Get(string key)
        {
            if (key == null || !Table.TryGetValue(key, out var v))
                throw new KeyNotFoundException("ZombieText key not found: " + (key ?? "<null>"));
            return Language == ZombieLanguage.Ru ? v.Ru : v.En;
        }

        public static string KindName(ZombieKind kind)
        {
            if (!Kinds.TryGetValue(kind, out var v)) v = Kinds[ZombieKind.Unknown];
            return Language == ZombieLanguage.Ru ? v.Ru : v.En;
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
