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

        // Размер плашки и иконки подобран под игровой виджет «лайков» (👍 6/20) в левом верхнем углу.
        private const float PlateW = 168f;
        private const float PlateH = 42f;
        private const float IconSize = 32f;
        private const float TextLeftGlyph = 10f;
        private const float TextLeftImage = 48f;

        // Порядок слоя: ниже игрового затемнения переходов/загрузки (UIFade / UIFadeWithText = 800,
        // BLACKOUT_DEFAULT_SORTING_ORDER_VALUE), но выше обычного игрового UI — окон (LazyWindow, 400+),
        // тултипов (700) и курсора геймпада (701). Так чёрный экран перехода/загрузки накрывает наш
        // оверлей ровно как игровой HUD, а в игре он остаётся поверх окон.
        private const int CanvasSortingOrder = 760;

        // Иконка зомби в HUD: если у игры есть глиф для зоны воскрешения (WorldZoneDef.qualityIcon),
        // рисуем его прямо в тексте (как игра в заголовке зала), иначе — обычный спрайт-Image.
        private string _glyphName;
        private TMP_SpriteAsset _glyphAsset;
        private GameObject _iconGo;
        private float _glyphRetry;
        private bool _glyphApplied;

        // Стиль счётчика — как у игрового "👍 0/20" (HUD.happinessLabel): шрифт, материал, цвет.
        private bool _styled;
        private Color _normalColor = GameStyle.Text;

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
            canvas.sortingOrder = CanvasSortingOrder;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgImage = bg.GetComponent<Image>();
            // Плашка под иконкой/числом. Фон можно отключить (по умолчанию выключен), но прозрачный
            // Image остаётся — он ловит перетаскивание и клик по HUD.
            bgImage.color = Plugin.Mod.HudBackground != null && Plugin.Mod.HudBackground.Value
                ? new Color(0f, 0f, 0f, 0.5f)
                : new Color(0f, 0f, 0f, 0f);
            bgImage.raycastTarget = true;
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(0, 1);
            brt.pivot = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(Plugin.Mod.HudOffsetX.Value, -Plugin.Mod.HudOffsetY.Value);
            brt.sizeDelta = new Vector2(PlateW, PlateH);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(bg.transform, false);
            _iconGo = iconGo;
            _icon = iconGo.GetComponent<Image>();
            var irt = (RectTransform)iconGo.transform;
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0, 0.5f);
            irt.anchoredPosition = new Vector2(8, 0);
            irt.sizeDelta = new Vector2(IconSize, IconSize);
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
            rt.offsetMin = new Vector2(iconSprite != null ? TextLeftImage : TextLeftGlyph, 6);
            rt.offsetMax = new Vector2(-8, -6);

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
                Plugin.RefreshLanguage();
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
                int limit = ZombieRoster.Limit();

                bool gameActive = count >= 0 && ZombieRoster.GameReady() && ZombieRoster.GameHudVisible();
                bool show = gameActive && _visible;
                if (_canvasGo.activeSelf != show) _canvasGo.SetActive(show);
                if (!show || _text == null) return;

                EnsureGlyph();
                string body = HudFormat.CountShort(count, limit);
                if (_glyphAsset != null && !string.IsNullOrEmpty(_glyphName))
                {
                    _text.spriteAsset = _glyphAsset;
                    _text.text = "<sprite name=\"" + _glyphName + "\"> " + body;
                    ApplyIconMode(false);
                }
                else
                {
                    _text.text = body;
                    ApplyIconMode(true);
                }

                if (!_styled) ApplyGameStyle();

                // Лимит даёт игра (качество зоны воскрешения): "N / M", красным при превышении.
                _text.color = HudFormat.OverLimit(count, limit) ? GameStyle.Danger : _normalColor;
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("hud: " + ex.Message); }
        }

        // Копируем стиль с игрового лейбла "лайков"; размер оставляем из настройки мода.
        private void ApplyGameStyle()
        {
            var label = ZombieRoster.GameHappinessLabel();
            if (label == null || label.font == null) return;
            _text.font = label.font;
            _text.fontSharedMaterial = label.fontSharedMaterial;
            _text.fontStyle = label.fontStyle;
            _normalColor = label.color;
            _styled = true;
            Plugin.Log.LogInfo("hud: style from happinessLabel font=" + label.font.name + " color=" + label.color);
        }

        // Иконку ищем лениво (зона доступна только с загруженным сейвом) и кэшируем;
        // если глиф не нашёлся, раз в 30 секунд пробуем ещё раз.
        private void EnsureGlyph()
        {
            if (_glyphApplied) return;
            if (Time.realtimeSinceStartup < _glyphRetry) return;
            _glyphRetry = Time.realtimeSinceStartup + 30f;
            var name = ZombieRoster.QualityIcon();
            if (string.IsNullOrEmpty(name)) return;
            _glyphName = name;
            _glyphAsset = GameStyle.SpriteAssetFor(name);
            _glyphApplied = _glyphAsset != null;
            Plugin.Log.LogInfo("hud: zombie glyph = " + name + (_glyphApplied ? " (ok)" : " (нет в спрайт-ассетах)"));
        }

        // Иконка-Image и глиф в тексте взаимоисключающие (иначе значок будет дважды).
        private void ApplyIconMode(bool imageIcon)
        {
            if (_iconGo != null && _iconGo.activeSelf != imageIcon) _iconGo.SetActive(imageIcon);
            var rt = _text.rectTransform;
            float left = imageIcon ? TextLeftImage : TextLeftGlyph;
            if (!Mathf.Approximately(rt.offsetMin.x, left)) rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }
    }
}
