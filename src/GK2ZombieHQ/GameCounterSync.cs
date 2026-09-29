using System;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Настройка Fix → SyncGameCounter (по умолчанию выкл).
    // Игра держит своё число зомби в ресурсе игрока cur_zombies_count: +1 при каждом воскрешении,
    // −1 — своими способами избавиться от зомби. Если зомби убрал другой мод (например, сжёг),
    // счётчик игры остаётся завышенным, а с ним и штраф debuff_excessive_zombie (игра сравнивает
    // счётчик с качеством зоны воскрешения). При включённой настройке пишем в cur_zombies_count
    // реальное число зомби с телом в мире (то же, что считает наш HUD/панель) и пересчитываем
    // штраф ровно как функция игры TrySetOrRemoveResurrectionDebuff.
    internal static class GameCounterSync
    {
        private const string CounterKey = "cur_zombies_count";
        private const string DebuffPerk = "debuff_excessive_zombie";
        private const float IntervalSeconds = 3f;

        private static float _nextCheck;

        internal static bool Enabled
            => Plugin.Mod != null && Plugin.Mod.SyncGameCounter != null && Plugin.Mod.SyncGameCounter.Value;

        // Зовётся каждый кадр из ZombieHud.Update; работа — не чаще раза в 3 секунды.
        internal static void Tick()
        {
            if (!Enabled || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + IntervalSeconds;
            try
            {
                if (!ZombieRoster.GameReady()) return;
                var pd = MainGame.PlayerData;
                if (pd == null) return;

                int real = ZombieRoster.CountInWorld();
                if (real < 0) return;
                int game = pd.GetResInt(CounterKey);
                if (game == real) return;

                pd.SetRes(CounterKey, real);
                UpdateDebuff(real);
                Plugin.Log.LogInfo("game zombie counter synced: " + game + " -> " + real);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("counter sync: " + ex.Message); }
        }

        // Как TrySetOrRemoveResurrectionDebuff: счётчик ≤ качества зоны воскрешения — штрафа нет.
        private static void UpdateDebuff(int count)
        {
            var zone = MainGame.WorldData?.GetWorldZoneDataById("resurrection");
            var perks = MainGame.Instance?.GameSave?.perkSystemData;
            if (zone == null || perks == null) return;
            int limit = (int)zone.GetTotalQuality();
            if (count <= limit) perks.RemovePerk(DebuffPerk);
            else if (!perks.HasPerk(DebuffPerk)) perks.AddPerk(DebuffPerk);
        }
    }
}
