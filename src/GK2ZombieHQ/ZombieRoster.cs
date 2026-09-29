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

        // Сейв загружен и игра идёт? В главном меню PlayerData может остаться от прошлой сессии,
        // поэтому дополнительно проверяем состояние игры (MainMenu / InGame) — иначе HUD и панель
        // висят в меню.
        internal static bool GameReady()
        {
            try
            {
                if (MainGame.PlayerData == null) return false;
                var mg = MainGame.Instance;
                return mg != null && mg.gameState == MainGame.GameState.InGame;
            }
            catch { return false; }
        }

        // Игровой HUD скрыт? Игра прячет свой HUD (катсцены, загрузка/переходы, режим стройки,
        // главное меню) через HUD.SetDisableState(...) -> LazyWidgetBase.Hide() -> SetActive(false).
        // Наш оверлей отдельный, поэтому в этих фазах он оставался висеть. Зеркалим состояние
        // игрового HUD. Если HUD ещё не создан — не прячем (fail-safe, прежнее поведение).
        private static global::HUD _gameHud;

        internal static bool GameHudVisible()
        {
            try
            {
                if (_gameHud == null)
                {
                    var all = UnityEngine.Resources.FindObjectsOfTypeAll<global::HUD>();
                    if (all != null && all.Length > 0) _gameHud = all[0];
                }
                if (_gameHud == null) return true;
                return _gameHud.gameObject.activeInHierarchy;
            }
            catch { return true; }
        }

        // Игровой лейбл "лайков" (👍 0/20) в HUD — образец шрифта и цвета для нашего счётчика.
        private static readonly System.Reflection.FieldInfo HappinessLabelField =
            HarmonyLib.AccessTools.Field(typeof(global::HUD), "happinessLabel");

        internal static TMPro.TextMeshProUGUI GameHappinessLabel()
        {
            try
            {
                GameHudVisible(); // находит и кэширует _gameHud
                return _gameHud != null && HappinessLabelField != null
                    ? HappinessLabelField.GetValue(_gameHud) as TMPro.TextMeshProUGUI : null;
            }
            catch { return null; }
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

        // Имя спрайта-иконки зомби из определения зоны воскрешения (WorldZoneDef.qualityIcon) —
        // тот самый значок, который игра рисует в заголовке зала воскрешения ("16/20").
        internal static string QualityIcon()
        {
            try
            {
                var world = MainGame.WorldData;
                if (world == null) return null;
                var zone = world.GetWorldZoneDataById("resurrection");
                var def = zone != null ? zone.Definition : null;
                var icon = def != null ? def.qualityIcon : null;
                return string.IsNullOrEmpty(icon) ? null : icon;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("quality icon: " + ex.Message); return null; }
        }

        // Лимит зомби от игры: качество зоны воскрешения — столько зомби можно держать,
        // дальше игра вешает дебафф debuff_excessive_zombie. -1 = лимита нет/неизвестен.
        internal static int Limit()
        {
            try
            {
                var world = MainGame.WorldData;
                if (world == null) return -1;
                var zone = world.GetWorldZoneDataById("resurrection");
                if (zone == null) return -1;
                return HudFormat.LimitOf(zone.GetTotalQuality());
            }
            catch (Exception ex) { Plugin.Log.LogWarning("zombie limit: " + ex.Message); return -1; }
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

        // Где зомби: на станции, свободный, лежит на полу, в руках, на паллете/столе, в хоре,
        // либо данных нет без тела.
        internal static ZombieState StateOf(ZombieWgoData z)
        {
            if (z == null) return ZombieState.OffWorld;
            bool attached = false;
            try { attached = z.AttachedWgoData != null; } catch { }
            // Колесо (источник энергии) и гарнизон держат зомби на док-точке без привязки к
            // станции — это тоже работа, а не «без станции».
            if (!attached) attached = DockParent(z) != null;
            bool inScene = InScene(z);
            bool held = HeldByPlayer(z);
            bool bodyInWorld = !inScene && !held && IsDrop(z) && BodyInWorld(z);
            var place = (!inScene && !held && !bodyInWorld) ? PlaceOf(z) : ZombiePlace.None;
            return RosterLogic.Classify(inScene, bodyInWorld, held, place, attached);
        }

        // Столы/паллеты и места хора/органа, в чьём инвентаре лежит тело: игра хранит такие
        // трупы как предмет внутри WGO, а не как зомби в мире.
        private static readonly string[] TableContainers =
        {
            "resurrection_table_1", "resurrection_prepared", "pallet_corpse_1", "pallet_corpse_2", "autopsy_table_1"
        };

        private static readonly string[] ChoirContainers = { "zmb_choir_place", "zmb_organ_place" };

        internal static ZombiePlace PlaceOf(ZombieWgoData z)
        {
            if (ContainerOf(z, TableContainers) != null) return ZombiePlace.Table;
            if (ContainerOf(z, ChoirContainers) != null) return ZombiePlace.Choir;
            return ZombiePlace.None;
        }

        // Стол/паллета/место хора, в чьём инвентаре лежит тело зомби, или null.
        private static WgoData ContainerOf(ZombieWgoData z, string[] ids)
        {
            try
            {
                var item = z != null ? z.ZombieItem : null;
                if (item == null) return null;
                var world = MainGame.WorldData;
                if (world == null) return null;
                foreach (var id in ids)
                {
                    var list = world.GetWgoDataList(id);
                    if (list == null) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var wgo = list[i];
                        if (wgo == null) continue;
                        if (InventoryHas(wgo.Inventory, item)) return wgo;
                    }
                }
            }
            catch { }
            return null;
        }

        // Тело лежит либо во внешнем инвентаре стола (Inventory.Data.Inventory),
        // либо прямо в инвентаре WGO (места хора/органа — игра ищет их по группе "zombie").
        private static bool InventoryHas(Inventory inv, Item item)
        {
            if (inv == null || item == null) return false;
            try
            {
                var data = inv.Data;
                var items = data != null ? data.Inventory : null;
                if (ListHas(items, item)) return true;
            }
            catch { }
            try
            {
                if (ListHas(inv.GetItemsByGroupId("zombie"), item)) return true;
            }
            catch { }
            return false;
        }

        private static bool ListHas(List<Item> items, Item item)
        {
            if (items == null) return false;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                try { if (it.UniqueId == item.UniqueId) return true; } catch { }
            }
            return false;
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

        // Дроп (тело на земле) этого зомби в загруженной сцене, или null.
        // У лежащего зомби WgoData.Position — последняя "станционная" позиция (лаборатория
        // воскрешения), а реальное место — позиция дропа.
        internal static DropView FindBodyDrop(ZombieWgoData z)
        {
            if (z == null) return null;
            try
            {
                var views = UnityEngine.Object.FindObjectsOfType<DropView>();
                if (views == null) return null;
                foreach (var v in views)
                    if (IsBodyDropOf(v, z)) return v;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("find body drop: " + ex.Message); }
            return null;
        }

        // Дроп всё ещё тело именно этого зомби (DropView переиспользуются пулом).
        internal static bool IsBodyDropOf(DropView v, ZombieWgoData z)
        {
            try
            {
                if (v == null || z == null || !v.isActiveAndEnabled) return false;
                if (!(v.InteractionHandler is ZombieDropInteractionHandler)) return false;
                var item = v.Data != null ? v.Data.Item : null;
                var sys = MainGame.ZombieSystemData;
                return item != null && sys != null && ReferenceEquals(sys.GetZombie(item.UniqueId), z);
            }
            catch { return false; }
        }

        // Где зомби на самом деле: в руках игрока — у игрока, лежит — у тела, в хоре/на
        // столе — у этого места (тело там предмет в инвентаре), иначе — WgoData.
        internal static UnityEngine.Vector3 WorldPosition(ZombieWgoData z, ref DropView bodyCache)
        {
            if (HeldByPlayer(z))
            {
                var pd = MainGame.PlayerData;
                if (pd != null && pd.position != null) return pd.position.Value;
            }
            // BodyInWorld — кэшированный (раз в 3 с) скан, поэтому полный поиск дропа
            // не идёт каждый кадр, когда тела в загруженной сцене нет.
            if (!IsBodyDropOf(bodyCache, z)) bodyCache = BodyInWorld(z) ? FindBodyDrop(z) : null;
            if (bodyCache != null && bodyCache.Data != null) return bodyCache.Data.Position;
            if (!InScene(z))
            {
                var place = ContainerOf(z, ChoirContainers) ?? ContainerOf(z, TableContainers);
                if (place != null) return place.Position;
            }
            return z.Position;
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
                    // "Нет тела в мире" не показываем: так выглядят и сожжённые в крематории
                    // (запись в сейве остаётся), и потерянные из-за бага — в списке им делать нечего.
                    // Их количество всё равно видно в заголовке панели ("всего M").
                    if (state == ZombieState.OffWorld) continue;

                    result.Add(new RosterEntry
                    {
                        Data = z,
                        Info = new ZombieInfo
                        {
                            Id = kv.Key.ToString(),
                            Name = LocalizeName(z.Name, kv.Key.ToString()),
                            Kind = OnWheel(z) ? ZombieKind.Wheel : MapKind(z.ZombieType),
                            WhiteSkulls = z.WhiteSkulls,
                            RedSkulls = z.RedSkulls,
                            PerksUsed = UsedPerks(z),
                            TechBlue = z.techBlue,
                            TechGreen = z.techGreen,
                            TechRed = z.techRed,
                            Gear = BuildGear(z),
                            Activity = z.WorkerActivity != null ? z.WorkerActivity.ToString() : null,
                            State = state,
                            // "Отозвать" (в переноску) осмысленно только для тех, у кого есть тело в мире
                            // и оно не на столе/в руках.
                            CanRecall = state == ZombieState.Working || state == ZombieState.Free || state == ZombieState.Lying
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
                // Лежащего на полу зомби забираем как игра по "[E] Взять": дроп -> в переноску.
                // PutZombieFromGameSceneToStoreForPlayer работает только с зомби в сцене.
                var body = FindBodyDrop(entry.Data);
                if (body != null)
                    return body.InteractionHandler.Interact();

                var z = entry.Data;
                var station = z.AttachedWgoData;
                if (station != null) ReleaseFromStation(z, station);
                ReleaseDockPoint(z);

                // Все штатные «взять» игры возвращают зомби интерактивность. Без этого
                // зомби со станций шахты/глины/песка/лесопилки (там IsInteractable=false)
                // потом «застревал» — например, после установки в гарнизон.
                z.IsInteractable = true;
                try { z.WorldZoneData?.NotifyWgoDataChanged(); } catch { }
                MainGame.ZombieSystemData.PutZombieFromGameSceneToStoreForPlayer(pd, z);
                return true;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("recall: " + ex); return false; }
        }

        // Как штатное «взять» у станции, к которой привязан зомби (см. Zombie*InteractionHandler).
        private static void ReleaseFromStation(ZombieWgoData z, WgoData station)
        {
            string id = station.id ?? "";
            if (TryReleaseBuilder(z, station, id)) return;

            if (z.ZombieType == ZombieType.Porter)
            {
                // Как PorterStationInteractionHandler: груз носильщика падает рядом с игроком.
                z.UnAttachFromWgoData(true);
                return;
            }

            // Как ZombieInteractionHandler: готовый автокрафт — довести и выгрузить.
            var craft = station.CraftComponent;
            if (craft != null && craft.Status == CraftComponentStatus.ReadyToFinishAutoCraft)
            {
                craft.ContinueAutoCraft();
                var items = station.CraftableObjectCraftInventory?.Data?.RemoveAllItems();
                if (items != null)
                    foreach (var item in items)
                        MainGame.Instance.dropSystem.DropItem(item, station.WorldId, station.Position);
                station.DropStoredTechPoints();
            }
            z.UnAttachFromWgoData(true);
        }

        // Шахта / глина / песок / лесопилка: зомби занимает «точку» стройки и носит предмет.
        // Повторяем Zombie{Mine,Clay,Sand,Sawmill}InteractionHandler.Interact (ветка «взять»).
        private static bool TryReleaseBuilder(ZombieWgoData z, WgoData station, string id)
        {
            string point, builder, endEvent;
            Action<WgoData> finish;
            if (id.Contains("sawmill")) { point = "sawmill_point"; builder = "builder_sawmill"; endEvent = "sawmill_craft_end"; finish = s => GK2.FlowCanvasNodes.Flow_FinishZombieSawmillCraft.FinishCraft(s, false); }
            else if (id.Contains("mine")) { point = "mine_point"; builder = "builder_mine"; endEvent = "mine_craft_end"; finish = s => GK2.FlowCanvasNodes.Flow_FinishZombieMineCraft.FinishCraft(s, false); }
            else if (id.Contains("clay")) { point = "clay_point"; builder = "builder_clay_sand"; endEvent = "clay_craft_end"; finish = s => GK2.FlowCanvasNodes.Flow_FinishZombieClayCraft.FinishCraft(s, false); }
            else if (id.Contains("sand")) { point = "sand_point"; builder = "builder_clay_sand"; endEvent = "sand_craft_end"; finish = s => GK2.FlowCanvasNodes.Flow_FinishZombieSandCraft.FinishCraft(s, false); }
            else return false;

            if (z.GameResStr.Has(point))
            {
                MainGame.WorldData.GetWgoData(builder)?.SetGameRes(z.GameResStr.Get(point), 0);
                z.GameResStr.Remove(point);
                z.FireEvent(endEvent);
            }
            else if (z.CaretakerPortableItem != null && !z.CaretakerPortableItem.IsEmpty)
            {
                finish(station);
                if (point == "sawmill_point") z.CaretakerPortableItem = Item.Empty;
            }
            station.SetGameRes("stuff_disabled", 0);
            station.CraftComponent?.Clear();
            z.UnAttachFromWgoData(true);
            Plugin.Log.LogInfo("recall: released from " + id);
            return true;
        }

        // Гарнизон (FightersContainer) и источник энергии держат зомби на док-точке без привязки
        // к станции — UnAttachFromWgoData там падал (AttachedWgoData == null), и отзыв не работал.
        // Как ZombieInteractionHandler / PowerSourceInteractionHandler: освобождаем точку.
        private static void ReleaseDockPoint(ZombieWgoData z)
        {
            try
            {
                if (SGuid.IsNullOrEmpty(z.takenDockPointsParentSGuid)) return;
                var parent = MainGame.Instance.GameSave.WorldData.GetWgoData(z.takenDockPointsParentSGuid);
                var dock = parent?.MainWgoPartData?.GetOccupiedDockPointBy(z.UniqueId);
                if (dock != null) z.UnOccupyDockPoint(dock);
                z.takenDockPointsParentSGuid = null;
                if (parent != null)
                {
                    try { GameScene.GetWgoViewGlobal(parent.UniqueId)?.DrawWidgets(); } catch { }
                    try { parent.WorldZoneData?.NotifyWgoDataChanged(); } catch { }
                }
                Plugin.Log.LogInfo("recall: released dock point of " + (parent != null ? parent.id : "?"));
            }
            catch (Exception ex) { Plugin.Log.LogWarning("recall dock: " + ex.Message); }
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

        // Красные черепа, потраченные на способности — то, что игра пишет на вкладке
        // "Способности N/M" (UIZombieWorkerWindow.RedrawPerksTabLabel).
        private static int UsedPerks(ZombieWgoData z)
        {
            try { return z.GetUsedPerksCount(); }
            catch { return 0; }
        }

        // Название предмета (ошейник) — по заголовку из игры, а не по id.
        private static string SafeItemHeader(Item item)
        {
            try { return item != null && item.Definition != null ? item.Definition.GetHeader() : null; }
            catch { return null; }
        }

        // Строка снаряжения зомби: ошейник/инструмент/броня + переносимые вещи (инъекции и т.п.).
        private const int MaxCarriedIcons = 8;

        private static List<GearIcon> BuildGear(ZombieWgoData z)
        {
            try
            {
                var gear = new List<GearIcon>
                {
                    GearItem(GearSlot.Collar, z.Collar),
                    GearItem(GearSlot.Tool, z.Hand),
                    GearItem(GearSlot.Armor, z.Armor),
                };

                // Переносимое — только у носильщика (см. GearLogic.ShowCarried).
                var carried = new List<GearIcon>();
                if (GearLogic.ShowCarried(MapKind(z.ZombieType)))
                {
                    var inv = z.WorkerInventory;
                    var data = inv != null ? inv.Data : null;
                    var items = data != null ? data.Inventory : null;
                    if (items != null)
                    {
                        foreach (var it in items)
                        {
                            if (it == null || it.IsEmpty) continue;
                            var def = it.Definition;
                            carried.Add(new GearIcon
                            {
                                Id = def != null ? def.id : null,
                                IconId = def != null ? def.iconId : null,
                                Name = SafeItemHeader(it),
                                Count = it.Count < 1 ? 1 : it.Count,
                            });
                        }
                    }
                }

                return GearLogic.Build(gear, carried, MaxCarriedIcons);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("gear: " + ex.Message);
                return GearLogic.Build(null, null, 0);
            }
        }

        private static GearIcon GearItem(GearSlot slot, Item item)
        {
            if (item == null || item.IsEmpty) return null;
            var def = item.Definition;
            return new GearIcon
            {
                Slot = slot,
                Id = def != null ? def.id : null,
                IconId = def != null ? def.iconId : null,
                Name = SafeItemHeader(item),
            };
        }

        // Объект, на док-точке которого стоит зомби (колесо, гарнизон), или null.
        internal static WgoData DockParent(ZombieWgoData z)
        {
            try
            {
                if (z == null || SGuid.IsNullOrEmpty(z.takenDockPointsParentSGuid)) return null;
                return MainGame.Instance.GameSave.WorldData.GetWgoData(z.takenDockPointsParentSGuid);
            }
            catch { return null; }
        }

        // Зомби крутит колесо в подвале (PowerSourceInteractionHandler).
        internal static bool OnWheel(ZombieWgoData z)
        {
            try
            {
                var parent = DockParent(z);
                return parent != null && parent.Definition != null
                    && parent.Definition.interactionType == WGODef.InteractionType.PowerSource;
            }
            catch { return false; }
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
