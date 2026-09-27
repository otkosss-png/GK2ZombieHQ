using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class HudFormatTests
    {
        [Fact] public void En_counts_with_limit() => Assert.Equal("Zombies: 7 / 10", HudFormat.Count(ZombieLanguage.En, 7, 10));
        [Fact] public void Ru_counts_with_limit() => Assert.Equal("Зомби: 7 / 10", HudFormat.Count(ZombieLanguage.Ru, 7, 10));
        [Fact] public void No_limit_shows_only_count() => Assert.Equal("Zombies: 3", HudFormat.Count(ZombieLanguage.En, 3, 0));
        [Fact] public void Negative_count_clamped() => Assert.Equal("Zombies: 0 / 5", HudFormat.Count(ZombieLanguage.En, -2, 5));
        [Fact] public void AtLimit_true_at_or_above() { Assert.True(HudFormat.AtLimit(10, 10)); Assert.True(HudFormat.AtLimit(11, 10)); Assert.False(HudFormat.AtLimit(9, 10)); }

        [Fact] public void Header_shows_limit_and_total()
            => Assert.Equal("Zombies: 11 / 20 · total 17", HudFormat.Header(ZombieLanguage.En, 11, 20, 17));

        [Fact] public void Header_hides_total_when_all_zombies_are_in_world()
            => Assert.Equal("Zombies: 16 / 20", HudFormat.Header(ZombieLanguage.En, 16, 20, 16));

        [Fact] public void Header_ru_shows_limit_and_total()
            => Assert.Equal("Зомби: 11 / 20 · всего 17", HudFormat.Header(ZombieLanguage.Ru, 11, 20, 17));

        [Fact] public void Header_without_limit_keeps_total()
            => Assert.Equal("Zombies: 3 · total 5", HudFormat.Header(ZombieLanguage.En, 3, 0, 5));

        [Fact] public void Header_without_limit_or_total()
            => Assert.Equal("Zombies: 4", HudFormat.Header(ZombieLanguage.En, 4, 0, 4));

        [Fact] public void Limit_from_game_is_int()
            => Assert.Equal(20, HudFormat.LimitOf(20.9f));

        [Fact] public void Limit_negative_means_unknown()
            => Assert.Equal(-1, HudFormat.LimitOf(-3f));
    }
}
