using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class ZombieTextTests
    {
        public ZombieTextTests() { ZombieText.Language = ZombieLanguage.En; }

        [Fact] public void Title_differs_by_language()
        {
            ZombieText.Language = ZombieLanguage.En;
            Assert.Equal("Zombie HQ", ZombieText.Get("Title"));
            ZombieText.Language = ZombieLanguage.Ru;
            Assert.Equal("Зомби-штаб", ZombieText.Get("Title"));
            ZombieText.Language = ZombieLanguage.En;
        }

        [Fact] public void Kind_names_are_localized()
        {
            Assert.Equal("Gardener", ZombieText.KindName(ZombieKind.Gardener));
            ZombieText.Language = ZombieLanguage.Ru;
            Assert.Equal("Садовник", ZombieText.KindName(ZombieKind.Gardener));
            ZombieText.Language = ZombieLanguage.En;
        }

        [Fact] public void Unknown_key_throws() => Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => ZombieText.Get("Nope"));

        [Fact] public void Status_name_localized_for_lying_free_hands_offworld()
        {
            Assert.Equal("on the floor", ZombieText.StatusName(ZombieState.Lying));
            Assert.Equal("no station", ZombieText.StatusName(ZombieState.Free));
            Assert.Equal("in hands", ZombieText.StatusName(ZombieState.InHands));
            Assert.Equal("on a pallet/table", ZombieText.StatusName(ZombieState.OnTable));
            Assert.Equal("in the church choir", ZombieText.StatusName(ZombieState.InChoir));
            Assert.Equal("no body in the world", ZombieText.StatusName(ZombieState.OffWorld));
            ZombieText.Language = ZombieLanguage.Ru;
            Assert.Equal("лежит на полу", ZombieText.StatusName(ZombieState.Lying));
            Assert.Equal("без станции", ZombieText.StatusName(ZombieState.Free));
            Assert.Equal("в руках", ZombieText.StatusName(ZombieState.InHands));
            Assert.Equal("на паллете/столе", ZombieText.StatusName(ZombieState.OnTable));
            Assert.Equal("в хоре", ZombieText.StatusName(ZombieState.InChoir));
            Assert.Equal("нет тела в мире", ZombieText.StatusName(ZombieState.OffWorld));
            ZombieText.Language = ZombieLanguage.En;
        }

        [Fact] public void Status_name_empty_for_working()
        {
            Assert.Null(ZombieText.StatusName(ZombieState.Working));
        }
    }
}
