using GK2ZombieHQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal sealed class ZombieHud : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private Image _icon;
        private GameObject _canvasGo;
        private float _timer;
        private bool _visible = true;

        private void Start()
        {
            try { BuildUi(); }
            catch (System.Exception ex) { Plugin.Log.LogWarning("hud build: " + ex); }
        }

        private void BuildUi()
        {
            _canvasGo = new GameObject("GK2ZombieHQ_HudCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);
            bgImage.raycastTarget = true;
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(0, 1);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(Plugin.Mod.HudOffsetX.Value, -Plugin.Mod.HudOffsetY.Value);
            brt.sizeDelta = new Vector2(300, 64);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(bg.transform, false);
            _icon = iconGo.GetComponent<Image>();
            var irt = (RectTransform)iconGo.transform;
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0, 0.5f);
            irt.anchoredPosition = new Vector2(10, 0);
            irt.sizeDelta = new Vector2(52, 52);
            var iconSprite = GameStyle.ZombieIcon;
            if (iconSprite != null)
            {
                _icon.sprite = iconSprite;
                _icon.preserveAspect = true;
                _icon.raycastTarget = false;
                _icon.color = Color.white;
            }
            else
            {
                iconGo.SetActive(false);
            }

            var textGo = new GameObject("Count", typeof(RectTransform));
            textGo.transform.SetParent(bg.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            if (GameStyle.Font != null) _text.font = GameStyle.Font;
            if (GameStyle.FontMaterial != null) _text.fontSharedMaterial = GameStyle.FontMaterial;
            _text.fontSize = Plugin.Mod.HudFontSize.Value;
            _text.fontStyle = FontStyles.Bold;
            _text.alignment = TextAlignmentOptions.Left;
            _text.color = GameStyle.Text;
            _text.raycastTarget = false;
            var rt = _text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(iconSprite != null ? 70 : 14, 8);
            rt.offsetMax = new Vector2(-14, -8);

            var drag = bg.AddComponent<HudDragHandler>();
            drag.CanvasRect = (RectTransform)_canvasGo.transform;
            drag.Target = brt;
            drag.Panel = GetComponent<ZombiePanel>();

            _visible = Plugin.Mod.HudEnabled.Value;
            _canvasGo.SetActive(_visible);
        }

        private void Update()
        {
            try
            {
                if (_canvasGo == null) return;
                if (Input.GetKeyDown(Plugin.Mod.HudToggleKey.Value.MainKey))
                {
                    _visible = !_visible;
                    _timer = 0f;
                }

                _timer -= Time.unscaledDeltaTime;
                if (_timer > 0f) return;
                _timer = 0.5f;

                int count = ZombieRoster.CountInWorld();

                bool gameActive = count >= 0;
                bool show = gameActive && _visible;
                if (_canvasGo.activeSelf != show) _canvasGo.SetActive(show);
                if (!show || _text == null) return;

                // Игра не отдаёт числовой лимит (порог дебафа молитвы) через API,
                // поэтому показываем только текущее количество зомби.
                _text.text = HudFormat.CountShort(count, 0);
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("hud: " + ex.Message); }
        }
    }
}
