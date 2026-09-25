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
    }
}
