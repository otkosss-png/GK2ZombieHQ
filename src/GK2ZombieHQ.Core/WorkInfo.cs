namespace GK2ZombieHQ.Core
{
    // Где и чем занят зомби: станция (название) и что происходит прямо сейчас.
    // Заполняется из данных игры (ZombieRoster.BuildWork), текст строит WorkLogic.Job.
    public sealed class WorkInfo
    {
        public string StationName;
        // Станция без «занятия» (колесо, гарнизон): показываем только её.
        public bool Passive;
        // Текущий крафт станции: название рецепта, предмет на выходе, прогресс 0..1 (<0 — неизвестно).
        public string CraftName;
        public string OutputId;
        public string OutputIconId;
        public int OutputCount;
        public float Progress = -1f;
        // Ключи ZombieText: помеха крафту ("job.NoResources"…) и текущее действие ("job.Planting"…).
        public string Problem;
        public string Task;

        public bool HasCraft => !string.IsNullOrEmpty(CraftName);
    }
}
