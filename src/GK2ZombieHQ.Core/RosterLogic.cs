using System;
using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public static class RosterLogic
    {
        public static void Sort(List<ZombieInfo> list)
        {
            if (list == null) return;
            list.Sort(Compare);
        }

        // Порядок: лежащие на полу, свободные, работающие, "в руках", "нет тела в мире";
        // внутри группы — по типу, затем по имени.
        public static int Compare(ZombieInfo a, ZombieInfo b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int s = Rank(a.State).CompareTo(Rank(b.State));
            if (s != 0) return s;
            int c = a.Kind.CompareTo(b.Kind);
            if (c != 0) return c;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        // Порядок в списке: лежащие на полу (их ищут), свободные, работающие,
        // затем "в руках" и "не в мире" (диагностика пропавших).
        private static int Rank(ZombieState state)
        {
            switch (state)
            {
                case ZombieState.Lying: return 0;
                case ZombieState.Free: return 1;
                case ZombieState.Working: return 2;
                case ZombieState.InHands: return 3;
                default: return 4;
            }
        }

        public static string Skulls(ZombieInfo z) => z == null ? "0/0" : z.WhiteSkulls + "/" + z.RedSkulls;

        // Состояние зомби по данным игры:
        //   held        — тело в переноске игрока (в руках);
        //   inScene     — есть в сцене (список зомби в мире / WorldData): на станции или свободный;
        //   bodyInWorld — тело-дроп реально лежит в мире (есть обработчик "[E] Взять");
        //   иначе       — данные есть, а тела в мире нет (стол воскрешения / хранилище / пропал).
        public static ZombieState Classify(bool inScene, bool bodyInWorld, bool held, bool attached)
        {
            if (held) return ZombieState.InHands;
            if (inScene) return attached ? ZombieState.Working : ZombieState.Free;
            if (bodyInWorld) return ZombieState.Lying;
            return ZombieState.OffWorld;
        }

        // Зомби учитывается в счётчике (его тело реально в мире).
        public static bool InWorld(ZombieState state) => state != ZombieState.OffWorld;
    }
}
