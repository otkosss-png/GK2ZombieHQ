using GK2ZombieHQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal sealed class ZombieHud : MonoBehaviour
    {
        private TextMeshProUGUI _text;
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

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_canvasGo.transform, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(0, 1);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(Plugin.Mod.HudOffsetX.Value, -Plugin.Mod.HudOffsetY.Value);
            brt.sizeDelta = new Vector2(340, 58);

            var textGo = new GameObject("Count", typeof(RectTransform));
            textGo.transform.SetParent(bg.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            if (GameStyle.Font != null) _text.font = GameStyle.Font;
            if (GameStyle.FontMaterial != null) _text.fontSharedMaterial = GameStyle.FontMaterial;
            _text.fontSize = Plugin.Mod.HudFontSize.Value;
            _text.fontStyle = FontStyles.Bold;
            _text.alignment = TextAlignmentOptions.Left;
            _text.color = GameStyle.Text;
            var rt = _text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);

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
                    _canvasGo.SetActive(_visible);
                }

                _timer -= Time.unscaledDeltaTime;
                if (_timer > 0f || _text == null || !_visible) return;
                _timer = 0.5f;

                int count = ZombieRoster.Count();
                _text.text = count < 0 ? "" : HudFormat.Count(ZombieText.Language, count, 0);
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("hud: " + ex.Message); }
        }
    }
}
