using System;
using System.Collections.Generic;
using GK2ZombieHQ.Core;
using LazyBearTechnology;

namespace GK2ZombieHQ
{
    internal sealed class RosterEntry
    {
        public ZombieInfo Info;
        public ZombieWgoData Data;
    }

    internal static class ZombieRoster
    {
        internal static int Count()
        {
            try
            {
                var pd = MainGame.PlayerData;
                return pd == null ? -1 : pd.GetResInt("cur_zombies_count");
            }
            catch (Exception ex) { Plugin.Log.LogWarning("zombie count: " + ex.Message); return -1; }
        }

        internal static int Limit()
        {
            try
            {
                var pd = MainGame.PlayerData;
                return pd == null ? -1 : pd.GetResInt("zombies_limit_mechanic");
            }
            catch (Exception ex) { Plugin.Log.LogWarning("zombie limit: " + ex.Message); return -1; }
        }

        internal static List<RosterEntry> Load()
        {
            var result = new List<RosterEntry>();
            try
            {
                var sys = MainGame.ZombieSystemData;
                if (sys == null || sys.Cache == null) return result;
                foreach (var kv in sys.Cache)
                {
                    var z = kv.Value;
                    if (z == null) continue;
                    result.Add(new RosterEntry
                    {
                        Data = z,
                        Info = new ZombieInfo
                        {
                            Id = kv.Key.ToString(),
                            Name = LocalizeName(z.Name, kv.Key.ToString()),
                            Kind = MapKind(z.ZombieType),
                            WhiteSkulls = z.WhiteSkulls,
                            RedSkulls = z.RedSkulls,
                            Collar = SafeItemHeader(z.Collar),
                            Activity = z.WorkerActivity != null ? z.WorkerActivity.ToString() : null,
                            // ОТКЛЮЧЕНО: «Отозвать» через PutZombieFromGameSceneToStoreForPlayer ломает
                            // рабочую станцию (козлы и т.п.) — сначала нужно корректно отвязать зомби.
                            CanRecall = false
                        }
                    });
                }
                result.Sort((a, b) =>
                {
                    int c = a.Info.Kind.CompareTo(b.Info.Kind);
                    return c != 0 ? c : string.Compare(a.Info.Name, b.Info.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex) { Plugin.Log.LogWarning("roster load: " + ex); }
            return result;
        }

        internal static bool Recall(RosterEntry entry)
        {
            // ОТКЛЮЧЕНО (опасно): прямой перенос в стор не отвязывает зомби от станции и ломает её.
            Plugin.Log.LogWarning("recall disabled: unsafe (station would break)");
            return false;
        }

        internal static void OpenWindow(RosterEntry entry)
        {
            try
            {
                if (entry == null || entry.Data == null) return;
                LazyUI.GetWindow<UIZombieWorkerWindow>().Open(new UIZombieWorkerWindowData(entry.Data));
            }
            catch (Exception ex) { Plugin.Log.LogWarning("open zombie window: " + ex); }
        }

        // Имя зомби в игре — ключ локализации (zombie_name_29); превращаем в читаемое.
        private static string LocalizeName(string name, string fallback)
        {
            if (string.IsNullOrEmpty(name)) return fallback;
            try
            {
                var loc = LLBase.L(name);
                return string.IsNullOrEmpty(loc) ? name : loc;
            }
            catch { return name; }
        }

        // Название предмета (ошейник) — по заголовку из игры, а не по id.
        private static string SafeItemHeader(Item item)
        {
            try { return item != null && item.Definition != null ? item.Definition.GetHeader() : null; }
            catch { return null; }
        }

        private static ZombieKind MapKind(ZombieType t)
        {
            switch (t)
            {
                case ZombieType.Free: return ZombieKind.Free;
                case ZombieType.Crafter: return ZombieKind.Crafter;
                case ZombieType.Caretaker: return ZombieKind.Caretaker;
                case ZombieType.ConveyorCrafter: return ZombieKind.ConveyorCrafter;
                case ZombieType.Worker: return ZombieKind.Worker;
                case ZombieType.Porter: return ZombieKind.Porter;
                case ZombieType.Gardener: return ZombieKind.Gardener;
                case ZombieType.ConveyorTransporter: return ZombieKind.ConveyorTransporter;
                case ZombieType.Fighter: return ZombieKind.Fighter;
                default: return ZombieKind.Unknown;
            }
        }
    }
}
