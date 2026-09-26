using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Пока открыта наша панель (или режим камеры): игровой ввод не обрабатываем.
    // Esc/B закрывает панель и выходит из камеры; геймпадная GameKey открывает панель.
    [HarmonyPatch(typeof(PlayerInputHandler), "UpdateInput")]
    internal static class PlayerInputHandler_UpdateInput_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            var cam = ZombieCameraFollow.Instance;
            if (cam != null && cam.IsActive)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || LazyInput.GetKeyDown(GameKey.InGameMenu)) cam.Exit();
                return false;
            }

            var panel = ZombiePanel.Instance;
            if (panel == null) return true;

            if (panel.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) panel.Close();
                return false;
            }

            if (ZombiePanel.TryGamepadOpen())
            {
                panel.Toggle();
                return false;
            }

            return true;
        }
    }
}
