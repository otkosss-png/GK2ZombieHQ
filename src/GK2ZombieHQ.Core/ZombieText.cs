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
            ["Close"] = ("Close", "Закрыть"),
            ["NoZombies"] = ("No zombies", "Зомби нет"),
            ["Free"] = ("free", "свободен"),
            ["Error"] = ("Error", "Ошибка"),
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
    }
}
