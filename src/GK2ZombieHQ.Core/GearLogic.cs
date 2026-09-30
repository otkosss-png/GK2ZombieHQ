using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public enum GearSlot
    {
        None = 0,
        Collar = 1,
        Tool = 2,
        Armor = 3,
    }

    // Значок в строке снаряжения: слот экипировки либо переносимый предмет.
    public sealed class GearIcon
    {
        public GearSlot Slot;
        public string Id;
        // Имя спрайта предмета (ItemDef.iconId); если пусто — пробуем Id.
        public string IconId;
        public string Name;
        public int Count = 1;

        public bool IsEmpty => string.IsNullOrEmpty(Id);
    }

    // Сборка строки снаряжения для панели: три слота экипировки (в фиксированном порядке,
    // пустые остаются как заглушки) + переносимые предметы (одинаковые склеиваются в один
    // значок с количеством, лишние обрезаются).
    public static class GearLogic
    {
        // Переносимые вещи показываем только у носильщика: игра отдаёт WorkerInventory =
        // собственный инвентарь зомби только для ZombieType.Porter (6), а для остальных —
        // инвентарь станции (рабочие материалы вроде урожая), которые к «снаряжению» не относятся.
        public static bool ShowCarried(ZombieKind kind) => kind == ZombieKind.Porter;

        // Первые слоты Build — снаряжение (ошейник/инструмент/броня), дальше — переносимое.
        public const int EquipSlotCount = 3;

        public static List<GearIcon> Build(
            IList<GearIcon> gear, IList<GearIcon> carried, int maxCarried)
        {
            var result = new List<GearIcon>();

            foreach (var slot in new[] { GearSlot.Collar, GearSlot.Tool, GearSlot.Armor })
            {
                GearIcon found = null;
                if (gear != null)
                {
                    foreach (var g in gear)
                        if (g != null && g.Slot == slot && !g.IsEmpty) { found = g; break; }
                }
                result.Add(found ?? new GearIcon { Slot = slot });
            }

            var merged = new List<GearIcon>();
            if (carried != null)
            {
                foreach (var c in carried)
                {
                    if (c == null || c.IsEmpty) continue;
                    GearIcon existing = null;
                    foreach (var m in merged)
                        if (m.Id == c.Id) { existing = m; break; }
                    if (existing == null)
                    {
                        merged.Add(new GearIcon { Slot = GearSlot.None, Id = c.Id, IconId = c.IconId, Name = c.Name, Count = c.Count < 1 ? 1 : c.Count });
                    }
                    else
                    {
                        existing.Count += c.Count < 1 ? 1 : c.Count;
                        if (string.IsNullOrEmpty(existing.IconId)) existing.IconId = c.IconId;
                        if (string.IsNullOrEmpty(existing.Name)) existing.Name = c.Name;
                    }
                }
            }

            int limit = maxCarried < 0 ? 0 : maxCarried;
            for (int i = 0; i < merged.Count && i < limit; i++) result.Add(merged[i]);
            return result;
        }
    }
}
