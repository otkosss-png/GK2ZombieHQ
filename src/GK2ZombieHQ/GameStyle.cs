using System;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    // Достаёт из сцены игровые визуальные ресурсы (шрифт, спрайт кнопки/панели),
    // чтобы наше окно выглядело в стиле Graveyard Keeper 2, а не «чёрным прямоугольником».
    internal static class GameStyle
    {
        private static bool _done;
        private static TMP_FontAsset _font;
        private static Material _fontMat;
        private static Sprite _buttonSprite;
        private static Sprite _panelSprite;

        internal static readonly Color Text = new Color(0.93f, 0.85f, 0.66f, 1f);
        internal static readonly Color Accent = new Color(1f, 0.66f, 0.33f, 1f);
        internal static readonly Color PanelBg = new Color(0.15f, 0.12f, 0.10f, 0.97f);
        internal static readonly Color ButtonBg = new Color(0.33f, 0.26f, 0.19f, 1f);
        internal static readonly Color Dim = new Color(0f, 0f, 0f, 0.62f);

        internal static TMP_FontAsset Font { get { Ensure(); return _font; } }
        internal static Material FontMaterial { get { Ensure(); return _fontMat; } }
        internal static Sprite ButtonSprite { get { Ensure(); return _buttonSprite; } }
        internal static Sprite PanelSprite { get { Ensure(); return _panelSprite; } }

        private static void Ensure()
        {
            if (_done) return;
            _done = true;

            try
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                {
                    if (t == null || t.font == null) continue;
                    _font = t.font;
                    _fontMat = t.fontSharedMaterial;
                    break;
                }
            }
            catch { }

            // Спрайт кнопки: берём с настоящей игровой кнопки (Button/LazyButton), не с ползунка.
            try
            {
                foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
                    if (TryGraphic(b != null ? b.targetGraphic as Image : null)) break;
            }
            catch { }
            if (_buttonSprite == null)
            {
                try
                {
                    foreach (var b in Resources.FindObjectsOfTypeAll<LazyButton>())
                        if (TryGraphic(b != null ? b.targetGraphic as Image : null)) break;
                }
                catch { }
            }

            // Фон панели: спрайт с «оконным» именем.
            try
            {
                foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
                {
                    var sp = img != null ? img.sprite : null;
                    if (sp == null || sp.name == null) continue;
                    var n = sp.name.ToLowerInvariant();
                    if (n.Contains("window") || n.Contains("popup") || n.Contains("panel")
                        || n.Contains("frame") || n.Contains("paper") || n.Contains("scroll_bg"))
                    {
                        _panelSprite = sp;
                        break;
                    }
                }
            }
            catch { }
        }

        private static bool TryGraphic(Image img)
        {
            var sp = img != null ? img.sprite : null;
            if (sp == null) return false;
            var r = sp.rect;
            // Отсекаем тонкие «полосочки» (разделители/ползунки).
            if (r.width < 16f || r.height < 16f) return false;
            _buttonSprite = sp;
            return true;
        }
    }
}
