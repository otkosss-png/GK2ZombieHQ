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
            _canvasGo.AddComponent<CanvasScaler>();

            var textGo = new GameObject("Count");
            textGo.transform.SetParent(_canvasGo.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = Plugin.Instance.HudFontSize.Value;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = Color.white;

            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(Plugin.Instance.HudOffsetX.Value, -Plugin.Instance.HudOffsetY.Value);
            rt.sizeDelta = new Vector2(420, 40);

            _visible = Plugin.Instance.HudEnabled.Value;
            _canvasGo.SetActive(_visible);
        }

        private void Update()
        {
            try
            {
                if (_canvasGo == null) return;
                if (Input.GetKeyDown(Plugin.Instance.HudToggleKey.Value))
                {
                    _visible = !_visible;
                    _canvasGo.SetActive(_visible);
                }

                _timer -= Time.unscaledDeltaTime;
                if (_timer > 0f || _text == null || !_visible) return;
                _timer = 0.5f;

                int count = ZombieRoster.Count();
                int limit = ZombieRoster.Limit();
                if (count < 0) { _text.text = ""; return; }
                _text.text = HudFormat.Count(ZombieText.Language, count, limit);
                _text.color = HudFormat.AtLimit(count, limit) ? new Color(1f, 0.4f, 0.4f) : Color.white;
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("hud: " + ex.Message); }
        }
    }
}
