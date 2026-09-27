using System;
using HarmonyLib;

namespace GK2ZombieHQ
{
    // Игра падает NRE внутри поведения смотрителя (ZombieWgoData.CaretakerTryPickUpFromInventory),
    // когда его цель/инвентарь исчезли: смотритель залипает, окно ошибки висит каждый кадр, а
    // состояние сохраняется в сейв (перезагрузка не лечит). Гвард глушит это исключение —
    // смотритель просто пропускает шаг, игра не падает. Счётчик виден в логе BepInEx.
    internal static class CaretakerGuard
    {
        internal static int Suppressed;
        private static float _nextSummary;

        internal static void Note(ZombieWgoData zombie, Exception ex)
        {
            Suppressed++;
            float now;
            try { now = UnityEngine.Time.realtimeSinceStartup; } catch { now = 0f; }
            if (Suppressed != 1 && now < _nextSummary) return;
            _nextSummary = now + 60f;
            string name = "";
            try { name = zombie != null ? zombie.Name : ""; } catch { }
            Plugin.Log.LogWarning("смотритель: подавлена ошибка игры ("
                + (string.IsNullOrEmpty(name) ? "?" : name) + "): " + ex.GetType().Name
                + "; всего подавлено: " + Suppressed);
        }
    }

    [HarmonyPatch(typeof(ZombieWgoData), "CaretakerTryPickUpFromInventory")]
    internal static class CaretakerTryPickUpFromInventory_Guard
    {
        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, ZombieWgoData __instance)
        {
            if (__exception == null) return null;
            try
            {
                if (Plugin.Mod != null && Plugin.Mod.CaretakerGuard != null && !Plugin.Mod.CaretakerGuard.Value)
                    return __exception;
                CaretakerGuard.Note(__instance, __exception);
                return null;
            }
            catch { return __exception; }
        }
    }
}
