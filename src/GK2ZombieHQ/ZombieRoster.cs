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
                            Name = string.IsNullOrEmpty(z.Name) ? kv.Key.ToString() : z.Name,
                            Kind = MapKind(z.ZombieType),
                            WhiteSkulls = z.WhiteSkulls,
                            RedSkulls = z.RedSkulls,
                            Collar = z.Collar != null ? SafeItemName(z.Collar) : null,
                            Activity = z.WorkerActivity != null ? z.WorkerActivity.ToString() : null,
                            CanRecall = true
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
            try
            {
                if (entry == null || entry.Data == null) return false;
                MainGame.ZombieSystemData
                    .PutZombieFromGameSceneToStoreForPlayer(MainGame.PlayerData, entry.Data);
                return true;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("recall: " + ex); return false; }
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

        private static string SafeItemName(Item item)
        {
            try { return item != null && item.Definition != null ? item.Definition.id : null; } catch { return null; }
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
