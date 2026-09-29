using System.Collections.Generic;
using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    // ZombieText — статическое состояние: тесты не должны идти параллельно с другими, кто его меняет.
    [Collection("ZombieText")]
    public class ZombieTextTests
    {
        public ZombieTextTests() { ZombieText.Use("en"); }

        [Fact] public void Title_differs_by_language()
        {
            Assert.Equal("Zombie HQ", ZombieText.Get("Title"));
            ZombieText.Use("ru");
            Assert.Equal("Зомби-штаб", ZombieText.Get("Title"));
            ZombieText.Use("en");
        }

        [Fact] public void Kind_names_are_localized()
        {
            Assert.Equal("Gardener", ZombieText.KindName(ZombieKind.Gardener));
            ZombieText.Use("ru");
            Assert.Equal("Садовник", ZombieText.KindName(ZombieKind.Gardener));
            ZombieText.Use("en");
        }

        [Fact] public void Unknown_key_throws() => Assert.Throws<KeyNotFoundException>(() => ZombieText.Get("Nope"));

        [Fact] public void Status_name_localized_for_lying_free_hands_offworld()
        {
            Assert.Equal("on the floor", ZombieText.StatusName(ZombieState.Lying));
            Assert.Equal("no station", ZombieText.StatusName(ZombieState.Free));
            Assert.Equal("in hands", ZombieText.StatusName(ZombieState.InHands));
            Assert.Equal("on a pallet/table", ZombieText.StatusName(ZombieState.OnTable));
            Assert.Equal("in the church choir", ZombieText.StatusName(ZombieState.InChoir));
            Assert.Equal("no body in the world", ZombieText.StatusName(ZombieState.OffWorld));
            ZombieText.Use("ru");
            Assert.Equal("лежит на полу", ZombieText.StatusName(ZombieState.Lying));
            Assert.Equal("без станции", ZombieText.StatusName(ZombieState.Free));
            Assert.Equal("в руках", ZombieText.StatusName(ZombieState.InHands));
            Assert.Equal("на паллете/столе", ZombieText.StatusName(ZombieState.OnTable));
            Assert.Equal("в хоре", ZombieText.StatusName(ZombieState.InChoir));
            Assert.Equal("нет тела в мире", ZombieText.StatusName(ZombieState.OffWorld));
            ZombieText.Use("en");
        }

        [Fact] public void Status_name_empty_for_working()
        {
            Assert.Null(ZombieText.StatusName(ZombieState.Working));
        }

        [Fact] public void Unknown_language_uses_english()
        {
            ZombieText.Use("de");
            Assert.Equal("Recall", ZombieText.Get("Recall"));
            ZombieText.Use("en");
        }

        [Fact] public void Translation_file_overrides_and_falls_back()
        {
            ZombieText.Use("de", new Dictionary<string, string>
            {
                { "Recall", "Zurückrufen" },
                { "kind.Gardener", "Gärtner" },
                { "Open", "" },
            });
            Assert.Equal("de", ZombieText.Code);
            Assert.Equal("Zurückrufen", ZombieText.Get("Recall"));
            Assert.Equal("Gärtner", ZombieText.KindName(ZombieKind.Gardener));
            Assert.Equal("Open", ZombieText.Get("Open"));
            Assert.Equal("Camera", ZombieText.Get("Camera"));
            ZombieText.Use("en");
        }

        [Fact] public void Russian_file_falls_back_to_builtin_russian()
        {
            ZombieText.Use("ru", new Dictionary<string, string> { { "Recall", "Забрать" } });
            Assert.Equal("Забрать", ZombieText.Get("Recall"));
            Assert.Equal("Камера", ZombieText.Get("Camera"));
            ZombieText.Use("en");
        }

        [Fact] public void Template_has_every_key_for_en_and_ru()
        {
            var en = ZombieText.Template("en");
            var ru = ZombieText.Template("ru");
            foreach (var key in ZombieText.Keys)
            {
                Assert.False(string.IsNullOrEmpty(en[key]));
                Assert.False(string.IsNullOrEmpty(ru[key]));
            }
            Assert.Equal("Отозвать", ru["Recall"]);
        }
    }
}
