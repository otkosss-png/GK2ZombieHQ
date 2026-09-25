using System;
using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public static class RosterLogic
    {
        public static void Sort(List<ZombieInfo> list)
        {
            if (list == null) return;
            list.Sort((a, b) =>
            {
                int c = a.Kind.CompareTo(b.Kind);
                if (c != 0) return c;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        public static string Skulls(ZombieInfo z) => z == null ? "0/0" : z.WhiteSkulls + "/" + z.RedSkulls;
    }
}
