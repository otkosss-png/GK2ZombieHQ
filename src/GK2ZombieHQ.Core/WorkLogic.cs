using System;

namespace GK2ZombieHQ.Core
{
    // Текст «чем занят зомби» и перевод состояний игры в ключи ZombieText.
    public static class WorkLogic
    {
        // Порядок: действие (идёт, сажает, носит) → крафт станции → помеха → «ждёт работы».
        // Для пассивных станций (колесо, гарнизон) — null: там достаточно названия места.
        public static string Job(WorkInfo w)
        {
            if (w == null || w.Passive) return null;
            if (!string.IsNullOrEmpty(w.Task)) return ZombieText.Get(w.Task);
            if (w.HasCraft)
            {
                var text = w.CraftName;
                if (w.OutputCount > 1) text += " ×" + w.OutputCount;
                if (!string.IsNullOrEmpty(w.Problem)) return text + " · " + ZombieText.Get(w.Problem);
                if (w.Progress >= 0f) text += " · " + Percent(w.Progress) + "%";
                return text;
            }
            if (!string.IsNullOrEmpty(w.Problem)) return ZombieText.Get(w.Problem);
            return ZombieText.Get("job.Idle");
        }

        public static int Percent(float progress)
        {
            if (float.IsNaN(progress) || progress <= 0f) return 0;
            if (progress >= 1f) return 100;
            return (int)Math.Floor(progress * 100f);
        }

        // Состояние садовника/смотрителя/транспортёра (имя значения enum игры) → ключ действия.
        // "OnStation" и неизвестное — null: тогда показываем крафт станции или «ждёт работы».
        public static string TaskKey(string state)
        {
            if (string.IsNullOrEmpty(state) || state == "OnStation") return null;
            switch (state)
            {
                case "GoToStation": return "job.ToStation";
                case "FailedToFindPath": return "job.NoPath";
                case "CanNotPutItemToInventory": return "job.Full";
                case "WaitingOtherCaretakersOnInventory": return "job.Waiting";
            }
            if (state.Contains("Seeds")) return "job.Planting";
            if (state.Contains("Plants") || state == "WaitingForWgoDeath") return "job.Harvesting";
            if (state.StartsWith("GoTo", StringComparison.Ordinal) || state.Contains("PickingUp")) return "job.Carrying";
            return null;
        }

        // Статус крафта (имя значения CraftStatus) → помеха.
        public static string ProblemKey(string craftStatus)
        {
            switch (craftStatus)
            {
                case "NotEnoughResources": return "job.NoResources";
                case "NotEnoughFuel": return "job.NoFuel";
                case "DoesntHaveRequiredTool":
                case "DoesntHaveItemWithEnoughDurability": return "job.NoTool";
                case "NotEnoughSpaceInWgo":
                case "NotEnoughSpaceInMultiInventory": return "job.Full";
                default: return null;
            }
        }

        // Статус станции (CraftComponentStatus) → помеха: результат ждёт, когда его заберут.
        public static string StationProblemKey(string componentStatus)
        {
            switch (componentStatus)
            {
                case "WaitingForWorkerPickUp":
                case "WaitingForOutputDrop": return "job.WaitPickup";
                default: return null;
            }
        }
    }
}
