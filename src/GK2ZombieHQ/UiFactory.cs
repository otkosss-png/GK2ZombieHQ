using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal static class UiFactory
    {
        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static Image PanelImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        internal static TextMeshProUGUI Label(string name, Transform parent, string text, int size,
            TextAlignmentOptions align, Color? color = null)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (GameStyle.Font != null) t.font = GameStyle.Font;
            if (GameStyle.FontMaterial != null) t.fontSharedMaterial = GameStyle.FontMaterial;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? GameStyle.Text;
            t.raycastTarget = false;
            return t;
        }

        internal static Button TextButton(string name, Transform parent, string text, int size)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            var sprite = GameStyle.ButtonSprite;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                img.color = Color.white;
            }
            else
            {
                img.color = GameStyle.ButtonBg;
            }
            img.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var label = Label("Label", rt, text, size, TextAlignmentOptions.Center, GameStyle.Text);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 2);
            lrt.offsetMax = new Vector2(-6, -2);
            return btn;
        }
    }
}
