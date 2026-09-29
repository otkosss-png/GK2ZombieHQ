using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    [Collection("ZombieText")]
    public class HudFormatTests
    {
        public HudFormatTests() { ZombieText.Use("en"); }

        [Fact] public void En_counts_with_limit() => Assert.Equal("Zombies: 7 / 10", HudFormat.Count(7, 10));

        [Fact] public void Ru_counts_with_limit()
        {
            ZombieText.Use("ru");
            Assert.Equal("Зомби: 7 / 10", HudFormat.Count(7, 10));
            ZombieText.Use("en");
        }

        [Fact] public void No_limit_shows_only_count() => Assert.Equal("Zombies: 3", HudFormat.Count(3, 0));
        [Fact] public void Negative_count_clamped() => Assert.Equal("Zombies: 0 / 5", HudFormat.Count(-2, 5));
        [Fact] public void AtLimit_true_at_or_above() { Assert.True(HudFormat.AtLimit(10, 10)); Assert.True(HudFormat.AtLimit(11, 10)); Assert.False(HudFormat.AtLimit(9, 10)); }

        [Fact] public void Limit_from_game_is_int()
            => Assert.Equal(20, HudFormat.LimitOf(20.9f));

        [Fact] public void Limit_negative_means_unknown()
            => Assert.Equal(-1, HudFormat.LimitOf(-3f));
    }
}
