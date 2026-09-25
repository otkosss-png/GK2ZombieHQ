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

        internal static Image PanelImage(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        internal static TextMeshProUGUI Label(string name, Transform parent, string text, int size, TextAlignmentOptions align)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        internal static Button TextButton(string name, Transform parent, string text, int size)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.25f, 0.95f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var label = Label("Label", rt, text, size, TextAlignmentOptions.Center);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
