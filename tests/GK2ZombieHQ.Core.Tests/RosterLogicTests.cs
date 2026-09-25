using System.Collections.Generic;
using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class RosterLogicTests
    {
        [Fact] public void Sort_groups_by_kind_then_name()
        {
            var list = new List<ZombieInfo>
            {
                new ZombieInfo { Name = "Zed", Kind = ZombieKind.Gardener },
                new ZombieInfo { Name = "Bob", Kind = ZombieKind.Fighter },
                new ZombieInfo { Name = "Ann", Kind = ZombieKind.Gardener },
            };
            RosterLogic.Sort(list);
            // Порядок Kind — как в enum (Gardener=7 раньше Fighter=9), внутри группы — по имени.
            Assert.Equal(new[] { "Ann", "Zed", "Bob" }, new[] { list[0].Name, list[1].Name, list[2].Name });
        }

        [Fact] public void Skulls_formats_w_r()
        {
            Assert.Equal("3/1", RosterLogic.Skulls(new ZombieInfo { WhiteSkulls = 3, RedSkulls = 1 }));
        }
    }
}
