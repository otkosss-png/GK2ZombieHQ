namespace GK2ZombieHQ.Core
{
    public static class HudFormat
    {
        public static string Count(ZombieLanguage lang, int count, int limit)
        {
            if (count < 0) count = 0;
            var label = lang == ZombieLanguage.Ru ? "Зомби" : "Zombies";
            if (limit <= 0) return label + ": " + count;
            return label + ": " + count + " / " + limit;
        }

        public static bool AtLimit(int count, int limit) => limit > 0 && count >= limit;

        // Короткий формат для HUD с иконкой: "N / L" (или только N, если лимит неизвестен).
        public static string CountShort(int count, int limit)
        {
            if (count < 0) count = 0;
            return limit > 0 ? count + " / " + limit : count.ToString();
        }

        public static bool OverLimit(int count, int limit) => limit > 0 && count > limit;

        // Заголовок панели: "Зомби: N", а если в сейве зомби больше, чем видно в мире,
        // показываем общее число — так видно "потерявшихся" (упавших/пропавших) зомби.
        public static string Header(ZombieLanguage lang, int count, int total)
        {
            var text = Count(lang, count, 0);
            if (total <= count || total <= 0) return text;
            return text + (lang == ZombieLanguage.Ru ? " · всего " : " · total ") + total;
        }
    }
}
