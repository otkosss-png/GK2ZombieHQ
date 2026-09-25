using System;
using GK2ZombieHQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    // Режим «камера следит за зомби»: камера закрепляется за зомби, сверху — баннер
    // с именем и подсказкой, «Центр» — вернуть вид, Esc — выйти.
    internal sealed class ZombieCameraFollow : MonoBehaviour
    {
        internal static ZombieCameraFollow Instance;

        private GameObject _root;
        private TextMeshProUGUI _label;
        private WgoData _zombie;
        private Transform _savedTarget;

        internal bool IsActive => _zombie != null && _root != null && _root.activeSelf;

        private void Awake() { Instance = this; }

        private void Start()
        {
            try { BuildUi(); _root.SetActive(false); }
            catch (Exception ex) { Plugin.Log.LogWarning("camera ui: " + ex); }
        }

        internal void Follow(WgoData zombie, string displayName)
        {
            try
            {
                if (zombie == null) return;
                var cam = ActiveCamera();
                if (!IsActive && cam != null) _savedTarget = cam.Target;

                _zombie = zombie;
                if (_label != null) _label.text = ZombieText.Get("CameraFollow") + ": " + displayName;
                if (_root != null) _root.SetActive(true);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("camera follow: " + ex); }
        }

        internal void Exit()
        {
            try
            {
                _zombie = null;
                if (_root != null) _root.SetActive(false);
                var cam = ActiveCamera();
                if (cam != null && _savedTarget != null) cam.SetTargetInstant(_savedTarget);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("camera exit: " + ex); }
        }

        private static CameraController ActiveCamera()
        {
            var cs = CameraSystem.Instance;
            return cs != null ? cs.ActiveCameraController : null;
        }

        private void Center()
        {
            try
            {
                var cam = ActiveCamera();
                if (cam != null && _zombie != null) cam.SetPosition(_zombie.Position, 0f, null);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("camera center: " + ex); }
        }

        private void Update()
        {
            try
            {
                if (!IsActive) return;
                var cam = ActiveCamera();
                if (cam != null) cam.SetPosition(_zombie.Position, 0f, null);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("camera update: " + ex); }
        }

        private void BuildUi()
        {
            _root = new GameObject("GK2ZombieHQ_CameraBanner");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5200;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var panel = UiFactory.PanelImage("Panel", _root.transform, GameStyle.PanelBg, GameStyle.PanelSprite);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0, -20);
            prt.sizeDelta = new Vector2(760, 72);

            _label = UiFactory.Label("Label", prt, "", 30, TextAlignmentOptions.Left, GameStyle.Accent);
            var lrt = _label.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(20, 26); lrt.offsetMax = new Vector2(-320, -8);

            var hint = UiFactory.Label("Hint", prt, ZombieText.Get("EscExit"), 22, TextAlignmentOptions.Left, GameStyle.Text);
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(1, 0);
            hrt.pivot = new Vector2(0.5f, 0);
            hrt.offsetMin = new Vector2(20, 8); hrt.offsetMax = new Vector2(-320, 30);

            var center = UiFactory.TextButton("Center", prt, ZombieText.Get("Center"), 24);
            var crt = center.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 0.5f); crt.anchorMax = new Vector2(1, 0.5f);
            crt.pivot = new Vector2(1, 0.5f); crt.sizeDelta = new Vector2(160, 48);
            crt.anchoredPosition = new Vector2(-14, 0);
            center.onClick.AddListener(Center);
        }
    }
}
