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
        // затем "в руках", "на паллете/столе", "в хоре" и "нет тела в мире" (диагностика).
        private static int Rank(ZombieState state)
        {
            switch (state)
            {
                case ZombieState.Lying: return 0;
                case ZombieState.Free: return 1;
                case ZombieState.Working: return 2;
                case ZombieState.InHands: return 3;
                case ZombieState.OnTable: return 4;
                case ZombieState.InChoir: return 5;
                default: return 6;
            }
        }

        public static string Skulls(ZombieInfo z) => z == null ? "0/0" : z.WhiteSkulls + "/" + z.RedSkulls;

        // Красные черепа как на вкладке "Способности" окна зомби: "потрачено/всего" — "0/5".
        public static string RedSkulls(ZombieInfo z)
            => z == null ? "0/0" : z.PerksUsed + "/" + z.RedSkulls;

        // Заголовок строки панели: "N. Имя · Тип".
        public static string RowTitle(int number, string name, string kind)
        {
            var title = number + ". " + name;
            return string.IsNullOrEmpty(kind) ? title : title + " · " + kind;
        }

        // Очки технологий зомби (красные/зелёные/синие "кристаллы").
        // withGlyphs — рисовать игровыми глифами tech_red/tech_green/tech_blue через <sprite>.
        public static string Tech(ZombieInfo z, bool withGlyphs)
        {
            int blue = z != null ? z.TechBlue : 0;
            int green = z != null ? z.TechGreen : 0;
            int red = z != null ? z.TechRed : 0;
            if (!withGlyphs) return red + "  " + green + "  " + blue;
            return "<sprite name=\"tech_red\"> " + red
                + "  <sprite name=\"tech_green\"> " + green
                + "  <sprite name=\"tech_blue\"> " + blue;
        }

        // Состояние зомби по данным игры:
        //   held        — тело в переноске игрока (в руках);
        //   place       — тело лежит в контейнере: паллета/стол воскрешения либо хор/орган;
        //   inScene     — есть в сцене (список зомби в мире / WorldData): на станции или свободный;
        //   bodyInWorld — тело-дроп реально лежит в мире (есть обработчик "[E] Взять");
        //   иначе       — данные есть, а тела в мире нет (пропал из-за бага).
        public static ZombieState Classify(bool inScene, bool bodyInWorld, bool held, ZombiePlace place, bool attached)
        {
            if (held) return ZombieState.InHands;
            if (place == ZombiePlace.Table) return ZombieState.OnTable;
            if (place == ZombiePlace.Choir) return ZombieState.InChoir;
            if (inScene) return attached ? ZombieState.Working : ZombieState.Free;
            if (bodyInWorld) return ZombieState.Lying;
            return ZombieState.OffWorld;
        }

        // Зомби учитывается в счётчике (его тело реально в мире).
        public static bool InWorld(ZombieState state) => state != ZombieState.OffWorld;
    }
}
