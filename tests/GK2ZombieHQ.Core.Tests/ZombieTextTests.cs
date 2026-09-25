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
    }
}
