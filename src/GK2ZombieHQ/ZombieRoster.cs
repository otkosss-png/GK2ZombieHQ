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

        // Сейв загружен? В главном меню PlayerData == null — тогда мод молчит
        // (иначе HUD/панель активны в меню, а клик дёргает игру без сейва и роняет апдейт).
        internal static bool GameReady()
        {
            try { return MainGame.PlayerData != null; }
            catch { return false; }
        }

        // Число зомби, чьё тело реально в мире (работает, свободен или лежит на полу).
        // Данные без тела (стол воскрешения, хранилище, "призраки" от бага) не считаем.
        internal static int CountInWorld()
        {
            try
            {
                var sys = MainGame.ZombieSystemData;
                if (sys != null && sys.Cache != null)
                {
                    int n = 0;
                    foreach (var kv in sys.Cache) if (RosterLogic.InWorld(StateOf(kv.Value))) n++;
                    return n;
                }
                if (sys != null && sys.zombieOnSceneWgoIds != null) return sys.zombieOnSceneWgoIds.Count;
            }
            catch { }
            return Count();
        }

        // Сколько зомби вообще есть в сейве (включая лежащих и без тела) — для заголовка панели.
        internal static int Total()
        {
            try
            {
                var sys = MainGame.ZombieSystemData;
                if (sys != null && sys.Cache != null) return sys.Cache.Count;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("zombie total: " + ex.Message); }
            return -1;
        }

        // Зомби есть в сцене: в списке зомби в мире или в WorldData.
        internal static bool InScene(ZombieWgoData z)
        {
            try
            {
                if (z == null) return false;
                var sys = MainGame.ZombieSystemData;
                if (sys != null && sys.zombieOnSceneWgoIds != null && sys.zombieOnSceneWgoIds.Contains(z.UniqueId)) return true;
                var world = MainGame.WorldData;
                return world != null && world.GetWgoData(z.UniqueId) != null;
            }
            catch { return false; }
        }

        // Где зомби: на станции, свободный, лежит на полу, в руках или без тела в мире.
        internal static ZombieState StateOf(ZombieWgoData z)
        {
            if (z == null) return ZombieState.OffWorld;
            bool attached = false;
            try { attached = z.AttachedWgoData != null; } catch { }
            bool bodyInWorld = IsDrop(z) && BodyInWorld(z);
            return RosterLogic.Classify(InScene(z), bodyInWorld, HeldByPlayer(z), attached);
        }

        // Кэш тел, лежащих в мире: полный скан сцены не чаще раза в 3 секунды.
        private static readonly HashSet<Guid> BodiesInWorld = new HashSet<Guid>();
        private static float _bodiesNextScan;

        internal static bool BodyInWorld(ZombieWgoData z)
        {
            if (z == null) return false;
            RefreshBodies(false);
            return BodiesInWorld.Contains(z.UniqueId.Guid);
        }

        internal static void RefreshBodies(bool force)
        {
            try
            {
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (!force && now < _bodiesNextScan) return;
                _bodiesNextScan = now + 3f;
                BodiesInWorld.Clear();
                var views = UnityEngine.Object.FindObjectsOfType<DropView>();
                if (views == null) return;
                var sys = MainGame.ZombieSystemData;
                if (sys == null) return;
                foreach (var v in views)
                {
                    if (v == null) continue;
                    try
                    {
                        // Тело зомби — это дроп с обработчиком ZombieDropInteractionHandler
                        // (именно он даёт "[E] Взять"). Сам обработчик — не UnityEngine.Object,
                        // поэтому идём от дропа: DropData.Item -> ZombieWgoData.
                        if (!(v.InteractionHandler is ZombieDropInteractionHandler)) continue;
                        var data = v.Data;
                        var item = data != null ? data.Item : null;
                        if (item == null) continue;
                        var z = sys.GetZombie(item.UniqueId);
                        if (z != null) BodiesInWorld.Add(z.UniqueId.Guid);
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning("bodies scan: " + ex.Message); }
        }

        // Тело зомби лежит в переноске игрока (а не на земле).
        internal static bool HeldByPlayer(ZombieWgoData z)
        {
            try
            {
                var pd = MainGame.PlayerData;
                if (pd == null || z == null) return false;
                var item = z.ZombieItem;
                if (item == null) return false;
                var overhead = pd.OverheadItems;
                if (overhead == null) return false;
                for (int i = 0; i < overhead.Count; i++)
                {
                    var it = overhead[i];
                    if (it == null) continue;
                    if (ReferenceEquals(it, item)) return true;
                    try { if (it.UniqueId == item.UniqueId) return true; } catch { }
                }
            }
            catch { }
            return false;
        }

        // Зомби лежит на земле как "дроп" (тело), а не работает на станции.
        internal static bool IsDrop(ZombieWgoData z)
        {
            try
            {
                var sys = MainGame.ZombieSystemData;
                return z != null && sys != null && sys.zombieDrops != null && sys.zombieDrops.Contains(z);
            }
            catch { return false; }
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
                    // Показываем всех: и тех, кто в мире, и тех, у кого тела нет
                    // ("нет тела в мире" — так видны пропавшие из-за бага зомби).
                    var state = StateOf(z);

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
                            State = state,
                            // "Отозвать" (в переноску) осмысленно только для тех, у кого есть тело в мире.
                            CanRecall = state != ZombieState.OffWorld && state != ZombieState.InHands
                        }
                    });
                }
                result.Sort((a, b) => RosterLogic.Compare(a.Info, b.Info));
            }
            catch (Exception ex) { Plugin.Log.LogWarning("roster load: " + ex); }
            return result;
        }

        // Отзыв = как делает игра при переносе зомби: сначала отвязать от станции, потом в стор.
        // Порядок важен: прямой Put без UnAttach ломал рабочую станцию.
        internal static bool Recall(RosterEntry entry)
        {
            try
            {
                if (entry == null || entry.Data == null) return false;
                var pd = MainGame.PlayerData;
                if (pd == null) return false;
                if (!pd.HasFreeOverheadSlot)
                {
                    Plugin.Log.LogWarning("recall: нет свободного overhead-слота");
                    return false;
                }
                entry.Data.UnAttachFromWgoData(true);
                MainGame.ZombieSystemData.PutZombieFromGameSceneToStoreForPlayer(pd, entry.Data);
                return true;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("recall: " + ex); return false; }
        }

        // Включаем режим слежения камеры за зомби.
        internal static void FocusCamera(RosterEntry entry)
        {
            try
            {
                if (entry == null || entry.Data == null || ZombieCameraFollow.Instance == null) return;
                ZombieCameraFollow.Instance.Follow(entry.Data, entry.Info != null ? entry.Info.Name : "");
            }
            catch (Exception ex) { Plugin.Log.LogWarning("focus camera: " + ex); }
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
