using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public sealed class ZombieInfo
    {
        public string Id;
        public string Name;
        public ZombieKind Kind;
        public int WhiteSkulls;
        public int RedSkulls;
        // Сколько красных черепов потрачено на способности (ZombieWgoData.GetUsedPerksCount).
        public int PerksUsed;
        public int TechBlue;
        public int TechGreen;
        public int TechRed;
        public string Activity;
        public string Zone;
        public ZombieState State;
        public bool CanRecall;
        // Строка снаряжения: 3 слота (ошейник/инструмент/броня, пустые — заглушки) + переносимое.
        public List<GearIcon> Gear = new List<GearIcon>();
    }
}
