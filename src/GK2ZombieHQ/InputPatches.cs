using HarmonyLib;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Пока открыта наша панель: игровой ввод не обрабатываем (иначе Esc открывает
    // меню игры одновременно с закрытием панели). Esc здесь же закрывает панель.
    [HarmonyPatch(typeof(PlayerInputHandler), "UpdateInput")]
    internal static class PlayerInputHandler_UpdateInput_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            var panel = ZombiePanel.Instance;
            if (panel == null || !panel.IsOpen) return true;

            if (Input.GetKeyDown(KeyCode.Escape)) panel.Close();
            return false;
        }
    }
}
