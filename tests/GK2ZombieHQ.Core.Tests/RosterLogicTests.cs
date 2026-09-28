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

        [Fact] public void Classify_covers_all_places()
        {
            // В руках — даже если тело числится в дропах.
            Assert.Equal(ZombieState.InHands, RosterLogic.Classify(inScene: false, bodyInWorld: true, held: true, place: ZombiePlace.None, attached: false));
            // В сцене: со станцией — работает, без — свободный.
            Assert.Equal(ZombieState.Working, RosterLogic.Classify(inScene: true, bodyInWorld: false, held: false, place: ZombiePlace.None, attached: true));
            Assert.Equal(ZombieState.Free, RosterLogic.Classify(inScene: true, bodyInWorld: false, held: false, place: ZombiePlace.None, attached: false));
            // Тело-дроп реально лежит в мире.
            Assert.Equal(ZombieState.Lying, RosterLogic.Classify(inScene: false, bodyInWorld: true, held: false, place: ZombiePlace.None, attached: false));
            // Тело лежит на паллете/столе воскрешения.
            Assert.Equal(ZombieState.OnTable, RosterLogic.Classify(inScene: false, bodyInWorld: false, held: false, place: ZombiePlace.Table, attached: false));
            // Тело стоит в хоре/на органе.
            Assert.Equal(ZombieState.InChoir, RosterLogic.Classify(inScene: false, bodyInWorld: false, held: false, place: ZombiePlace.Choir, attached: false));
            // Данные есть, тела в мире нет (пропал из-за бага).
            Assert.Equal(ZombieState.OffWorld, RosterLogic.Classify(inScene: false, bodyInWorld: false, held: false, place: ZombiePlace.None, attached: false));
        }

        [Fact] public void Only_offworld_is_not_counted_in_world()
        {
            Assert.True(RosterLogic.InWorld(ZombieState.Working));
            Assert.True(RosterLogic.InWorld(ZombieState.Free));
            Assert.True(RosterLogic.InWorld(ZombieState.Lying));
            Assert.True(RosterLogic.InWorld(ZombieState.InHands));
            Assert.True(RosterLogic.InWorld(ZombieState.OnTable));
            Assert.True(RosterLogic.InWorld(ZombieState.InChoir));
            Assert.False(RosterLogic.InWorld(ZombieState.OffWorld));
        }

        [Fact] public void Tech_formats_with_glyphs_red_green_blue()
        {
            var z = new ZombieInfo { TechBlue = 0, TechGreen = 1, TechRed = 75 };
            Assert.Equal("<sprite name=\"tech_red\"> 75  <sprite name=\"tech_green\"> 1  <sprite name=\"tech_blue\"> 0",
                RosterLogic.Tech(z, withGlyphs: true));
        }

        [Fact] public void Tech_formats_without_glyphs_red_green_blue()
        {
            var z = new ZombieInfo { TechBlue = 2, TechGreen = 0, TechRed = 5 };
            Assert.Equal("5  0  2", RosterLogic.Tech(z, withGlyphs: false));
        }

        [Fact] public void Red_skulls_show_spent_on_perks_like_abilities_tab()
        {
            // Как вкладка "Способности 0/5": потрачено / всего красных черепов.
            Assert.Equal("0/5", RosterLogic.RedSkulls(new ZombieInfo { RedSkulls = 5, PerksUsed = 0 }));
            Assert.Equal("3/5", RosterLogic.RedSkulls(new ZombieInfo { RedSkulls = 5, PerksUsed = 3 }));
        }

        [Fact] public void Red_skulls_null_is_zero()
        {
            Assert.Equal("0/0", RosterLogic.RedSkulls(null));
        }

        [Fact] public void Row_number_prefixes_name()
        {
            Assert.Equal("1. Ann · Gardener", RosterLogic.RowTitle(1, "Ann", "Gardener"));
            Assert.Equal("12. Bob", RosterLogic.RowTitle(12, "Bob", null));
        }

        [Fact] public void Tech_null_is_zeroes()
        {
            Assert.Equal("0  0  0", RosterLogic.Tech(null, withGlyphs: false));
        }

        [Fact] public void Lying_zombies_come_first()
        {
            var list = new List<ZombieInfo>
            {
                new ZombieInfo { Name = "Aaa", Kind = ZombieKind.Crafter, State = ZombieState.Working },
                new ZombieInfo { Name = "Zzz", Kind = ZombieKind.Crafter, State = ZombieState.Lying },
            };
            RosterLogic.Sort(list);
            Assert.Equal("Zzz", list[0].Name);
        }

        [Fact] public void Offworld_zombies_go_last()
        {
            var list = new List<ZombieInfo>
            {
                new ZombieInfo { Name = "Ghost", Kind = ZombieKind.Crafter, State = ZombieState.OffWorld },
                new ZombieInfo { Name = "Choir", Kind = ZombieKind.Crafter, State = ZombieState.InChoir },
                new ZombieInfo { Name = "Pallet", Kind = ZombieKind.Crafter, State = ZombieState.OnTable },
                new ZombieInfo { Name = "Aaa", Kind = ZombieKind.Crafter, State = ZombieState.Working },
            };
            RosterLogic.Sort(list);
            Assert.Equal("Aaa", list[0].Name);
            Assert.Equal("Pallet", list[1].Name);
            Assert.Equal("Choir", list[2].Name);
            Assert.Equal("Ghost", list[3].Name);
        }
    }
}
