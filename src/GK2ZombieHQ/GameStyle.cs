using System;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal static class GameStyle
    {
        private static bool _done;
        private static TMP_FontAsset _font;
        private static Material _fontMat;
        private static Sprite _buttonSprite;
        private static Sprite _panelSprite;
        private static Sprite _zombieIcon;
        private static Sprite _whiteSkull, _redSkull;
        private static TMP_SpriteAsset _spriteAsset;

        internal static Sprite WhiteSkull { get { Ensure(); return _whiteSkull; } }
        internal static Sprite RedSkull { get { Ensure(); return _redSkull; } }
        // TMP-ассет спрайтов, в котором есть глифы "skull"/"rskull".
        internal static TMP_SpriteAsset SpriteAsset { get { Ensure(); return _spriteAsset; } }

        internal static readonly Color Text = new Color(0.93f, 0.85f, 0.66f, 1f);
        internal static readonly Color Accent = new Color(1f, 0.66f, 0.33f, 1f);
        internal static readonly Color Danger = new Color(1f, 0.35f, 0.30f, 1f);
        internal static readonly Color PanelBg = new Color(0.15f, 0.12f, 0.10f, 0.97f);
        internal static readonly Color ButtonBg = new Color(0.33f, 0.26f, 0.19f, 1f);
        internal static readonly Color Dim = new Color(0f, 0f, 0f, 0.62f);

        internal static TMP_FontAsset Font { get { Ensure(); return _font; } }
        internal static Material FontMaterial { get { Ensure(); return _fontMat; } }
        internal static Sprite ButtonSprite { get { Ensure(); return _buttonSprite; } }
        internal static Sprite PanelSprite { get { Ensure(); return _panelSprite; } }
        internal static Sprite ZombieIcon { get { Ensure(); return _zombieIcon; } }

        // Спрайт предмета из игрового справочника спрайтов. Игра берёт ItemDef.iconId
        // (см. UIItemCell.Draw: GetSprite(def.iconId, null)); если пусто — пробуем id.
        internal static Sprite ItemSprite(string id, string iconId)
        {
            try
            {
                var collection = EasySpritesCollection.Instance;
                if (collection == null) return null;
                if (!string.IsNullOrEmpty(iconId))
                {
                    var byIcon = collection.GetSprite(iconId, null);
                    if (byIcon != null) return byIcon;
                }
                if (!string.IsNullOrEmpty(id)) return collection.GetSprite(id, null);
                return null;
            }
            catch { return null; }
        }

        // Значки предметов нарисованы с синим контуром-заготовкой: игра рисует их материалом
        // ячейки UIItemCell.icon, шейдер которого перекрашивает синий в цвет из _Color
        // (ImageExtensions.BlueColorReplace(colors.NormalColor)). Обычный UI-материал оставляет
        // контур синим, поэтому берём материал у игровой ячейки — один общий на все значки.
        private static Material _itemIconMat;
        private static readonly int TintId = Shader.PropertyToID("_Color");

        internal static void ApplyItemIconMaterial(Image img)
        {
            if (img == null) return;
            var mat = ItemIconMaterial();
            if (mat != null) img.material = mat;
        }

        private static Material ItemIconMaterial()
        {
            if (_itemIconMat != null) return _itemIconMat;
            try
            {
                var iconField = HarmonyLib.AccessTools.Field(typeof(UIItemCell), "icon");
                var colorsField = HarmonyLib.AccessTools.Field(typeof(UIItemCell), "colors");
                if (iconField == null || colorsField == null) return null;
                foreach (var cell in Resources.FindObjectsOfTypeAll<UIItemCell>())
                {
                    var icon = cell != null ? iconField.GetValue(cell) as Image : null;
                    var colors = cell != null ? colorsField.GetValue(cell) as ImageColors : null;
                    var src = icon != null ? icon.material : null;
                    if (src == null || colors == null || !src.HasProperty(TintId)) continue;
                    var mat = new Material(src);
                    mat.SetColor(TintId, colors.NormalColor);
                    _itemIconMat = mat;
                    Plugin.Log.LogInfo("style: item icon material = " + src.name + " (" + (src.shader != null ? src.shader.name : "?") + ")");
                    break;
                }
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("style item icon material: " + ex.Message); }
            return _itemIconMat;
        }

        // TMP-ассет, в котором есть нужный глиф (например иконка зомби из зоны воскрешения).
        // Сначала берём уже найденный ассет со "skull", иначе ищем по всем спрайт-ассетам.
        internal static TMP_SpriteAsset SpriteAssetFor(string glyphName)
        {
            Ensure();
            if (string.IsNullOrEmpty(glyphName)) return null;
            if (_spriteAsset != null && _spriteAsset.GetSpriteIndexFromName(glyphName) >= 0) return _spriteAsset;
            foreach (var sa in Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>())
            {
                if (sa != null && sa.GetSpriteIndexFromName(glyphName) >= 0) return sa;
            }
            return null;
        }

        // Ищем спрайт игровой кнопки (LazyButton) — пересканируем при открытии панели,
        // т.к. в главном меню и в игре кнопки разные.
        internal static void RefreshButtonSprite()
        {
            try
            {
                foreach (var lb in Resources.FindObjectsOfTypeAll<LazyButton>())
                {
                    var img = lb != null ? lb.targetGraphic as Image : null;
                    var sp = img != null ? img.sprite : null;
                    if (sp == null || sp.name == null) continue;
                    var n = sp.name.ToLowerInvariant();
                    // Красная кнопка меню (как в «Паузе»): comm-btn-simple_red-active.
                    if (n.Contains("btn-simple") && n.Contains("red") && !n.Contains("trade") && !n.Contains("green"))
                    {
                        _buttonSprite = sp;
                        TakeMenuFont(lb);
                        break;
                    }
                }
                if (_buttonSprite != null) Plugin.Log.LogInfo("style: button sprite = " + _buttonSprite.name);
                else Plugin.Log.LogInfo("style: button sprite = (none, using solid)");

                // Черепа — глифы TMP (<sprite name="skull"/"rskull">). Ищем ассет, где они есть.
                _spriteAsset = TMP_Settings.defaultSpriteAsset;
                if (_spriteAsset == null || _spriteAsset.GetSpriteIndexFromName("skull") < 0)
                {
                    _spriteAsset = null;
                    foreach (var sa in Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>())
                    {
                        if (sa != null && sa.GetSpriteIndexFromName("skull") >= 0) { _spriteAsset = sa; break; }
                    }
                }
                Plugin.Log.LogInfo("style: skull glyph asset = " + (_spriteAsset != null ? _spriteAsset.name : "none"));
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("style scan: " + ex.Message); }
        }

        // Шрифт как в меню игры («Пауза»): берём у надписи красной кнопки меню шрифт вместе
        // с ЕГО материалом — чужой материал к шрифту может сделать текст невидимым.
        private static void TakeMenuFont(LazyButton lb)
        {
            try
            {
                var label = lb != null ? lb.GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null || label.font == null) return;
                _font = label.font;
                _fontMat = label.fontSharedMaterial;
                Plugin.Log.LogInfo("style: menu font = " + _font.name + " / " + (_fontMat != null ? _fontMat.name : "default"));
            }
            catch { }
        }

        // Перекрасить шрифтом меню все надписи под root (заголовки, строки, кнопки); цвета не трогаем.
        internal static void ApplyFont(Transform root)
        {
            if (root == null) return;
            Ensure();
            if (_font == null) return;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == null) continue;
                t.font = _font;
                if (_fontMat != null) t.fontSharedMaterial = _fontMat;
            }
        }

        private static void Ensure()
        {
            if (_done) return;
            _done = true;

            try
            {
                if (_font == null)
                    foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                    {
                        if (t == null || t.font == null) continue;
                        _font = t.font; _fontMat = t.fontSharedMaterial; break;
                    }
            }
            catch { }

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

            try
            {
                foreach (var img in Resources.FindObjectsOfTypeAll<Image>())
                {
                    var sp = img != null ? img.sprite : null;
                    if (sp == null || sp.name == null) continue;
                    var n = sp.name.ToLowerInvariant();
                    if (n.Contains("window") || n.Contains("popup") || n.Contains("panel")
                        || n.Contains("frame") || n.Contains("paper") || n.Contains("scroll_bg"))
                    { _panelSprite = sp; break; }
                }
            }
            catch { }

            // Иконка зомби из игрового спрайт-сбора.
            try
            {
                var col = LazySingletonSO<EasySpritesCollection>.Instance;
                if (col != null)
                {
                    foreach (var name in new[] { "body_zombie", "i_body", "i_zombie_1", "i_zombie_2", "zombie", "zombie_worker" })
                    {
                        var sp = col.GetSprite(name);
                        if (sp != null) { _zombieIcon = sp; break; }
                    }
                }
            }
            catch { }

            try
            {
                Plugin.Log.LogInfo("style: button=" + (_buttonSprite != null ? _buttonSprite.name : "solid")
                    + " panel=" + (_panelSprite != null ? _panelSprite.name : "solid")
                    + " zombieIcon=" + (_zombieIcon != null ? _zombieIcon.name : "none"));
            }
            catch { }
        }

        private static bool IsBadButtonSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var n = name.ToLowerInvariant();
            return n.Contains("red") || n.Contains("close") || n.Contains("delete")
                || n.Contains("cancel") || n.Contains("decline") || n.Contains("remove")
                || n.Contains("cross") || n.EndsWith("_x") || n.StartsWith("x_");
        }

        private static bool TryGraphic(Image img)
        {
            var sp = img != null ? img.sprite : null;
            if (sp == null) return false;
            var r = sp.rect;
            if (r.width < 16f || r.height < 16f) return false;
            if (IsBadButtonSprite(sp.name)) return false;
            _buttonSprite = sp;
            return true;
        }
    }
}
