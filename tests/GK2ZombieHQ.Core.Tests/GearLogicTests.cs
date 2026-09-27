using System.Collections.Generic;
using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class GearLogicTests
    {
        private static GearIcon Gear(GearSlot slot, string id) => new GearIcon { Slot = slot, Id = id, Name = id };

        [Fact]
        public void ShowsCarriedOnlyForPorter()
        {
            Assert.True(GearLogic.ShowCarried(ZombieKind.Porter));
            Assert.False(GearLogic.ShowCarried(ZombieKind.Gardener));
            Assert.False(GearLogic.ShowCarried(ZombieKind.Crafter));
            Assert.False(GearLogic.ShowCarried(ZombieKind.Caretaker));
        }
        [Fact]
        public void KeepsSlotOrderAndEmptyPlaceholders()
        {
            var gear = new List<GearIcon> { Gear(GearSlot.Armor, "armor_a"), Gear(GearSlot.Collar, "collar_a") };
            var row = GearLogic.Build(gear, null, 6);

            Assert.Equal(3, row.Count);
            Assert.Equal(GearSlot.Collar, row[0].Slot);
            Assert.Equal("collar_a", row[0].Id);
            Assert.Equal(GearSlot.Tool, row[1].Slot);
            Assert.True(row[1].IsEmpty);
            Assert.Equal(GearSlot.Armor, row[2].Slot);
            Assert.Equal("armor_a", row[2].Id);
        }

        [Fact]
        public void MergesCarriedItemsByIdAndSumsCounts()
        {
            var carried = new List<GearIcon>
            {
                new GearIcon { Id = "injection", Name = "Injection", Count = 1 },
                new GearIcon { Id = "injection", Name = "Injection", Count = 2 },
                new GearIcon { Id = "bandage", Name = "Bandage", Count = 1 },
            };
            var row = GearLogic.Build(null, carried, 6);

            var injections = row.Find(x => x.Id == "injection");
            Assert.NotNull(injections);
            Assert.Equal(3, injections.Count);
            Assert.Equal(1, row.FindAll(x => x.Id == "injection").Count);
            Assert.Equal(3 + 2, row.Count);
        }

        [Fact]
        public void TruncatesCarriedToLimit()
        {
            var carried = new List<GearIcon>();
            for (int i = 0; i < 10; i++) carried.Add(new GearIcon { Id = "item" + i, Name = "item" + i });
            var row = GearLogic.Build(null, carried, 4);

            Assert.Equal(3 + 4, row.Count);
            Assert.Equal("item3", row[row.Count - 1].Id);
        }

        [Fact]
        public void IgnoresNullsAndEmptyIds()
        {
            var carried = new List<GearIcon> { null, new GearIcon(), new GearIcon { Id = "ok" } };
            var row = GearLogic.Build(new List<GearIcon> { null }, carried, 6);

            Assert.Equal(4, row.Count);
            Assert.Equal("ok", row[3].Id);
            Assert.Equal(1, row[3].Count);
        }

        [Fact]
        public void KeepsAndMergesIconId()
        {
            var carried = new List<GearIcon>
            {
                new GearIcon { Id = "injection", IconId = "icon_injection", Count = 1 },
                new GearIcon { Id = "injection", Count = 2 },
            };
            var row = GearLogic.Build(null, carried, 6);
            var injection = row.Find(x => x.Id == "injection");
            Assert.Equal("icon_injection", injection.IconId);
            Assert.Equal(3, injection.Count);
        }
        [Fact]
        public void ZeroOrNegativeLimitKeepsOnlyGear()
        {
            var carried = new List<GearIcon> { new GearIcon { Id = "x" } };
            Assert.Equal(3, GearLogic.Build(null, carried, 0).Count);
            Assert.Equal(3, GearLogic.Build(null, carried, -5).Count);
        }
    }
}
