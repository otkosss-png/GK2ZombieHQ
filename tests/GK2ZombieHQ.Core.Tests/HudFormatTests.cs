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

        [Fact] public void Header_hides_total_when_all_zombies_are_in_world()
            => Assert.Equal("Zombies: 4", HudFormat.Header(ZombieLanguage.En, 4, 4));

        [Fact] public void Header_shows_total_when_some_zombies_are_missing()
            => Assert.Equal("Zombies: 3 · total 5", HudFormat.Header(ZombieLanguage.En, 3, 5));

        [Fact] public void Header_ru_shows_total()
            => Assert.Equal("Зомби: 3 · всего 5", HudFormat.Header(ZombieLanguage.Ru, 3, 5));

        [Fact] public void Header_ignores_unknown_total()
            => Assert.Equal("Zombies: 3", HudFormat.Header(ZombieLanguage.En, 3, 0));
    }
}
