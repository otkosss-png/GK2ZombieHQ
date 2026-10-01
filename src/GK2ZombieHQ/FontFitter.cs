using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Подбор шрифта под текст надписи. Наши надписи берут шрифт у игровой подписи окна, а в нём
    // может не быть символов перевода (корейский, японский, китайский — Нексус: вместо букв
    // пустые места). Если в текущем шрифте (с его запасными) символов нет — ищем среди
    // загруженных игрой шрифтов тот, где они есть, и ставим его с его же материалом.
    internal sealed class FontFitter : MonoBehaviour
    {
        private TMP_Text _label;
        private string _checkedText;
        private TMP_FontAsset _checkedFont;

        // Текст → подходящий шрифт (null = текущий годится). Кэш на всю сессию.
        private static readonly Dictionary<string, TMP_FontAsset> Cache = new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
        private static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);

        internal static void Attach(TMP_Text label)
        {
            if (label != null && label.GetComponent<FontFitter>() == null) label.gameObject.AddComponent<FontFitter>();
        }

        private void LateUpdate()
        {
            if (_label == null) _label = GetComponent<TMP_Text>();
            if (_label == null) return;
            // Перепроверяем, когда сменился текст или шрифт (код мода мог снова поставить шрифт окна).
            var text = _label.text;
            if (text == _checkedText && _label.font == _checkedFont) return;
            try { Fit(_label); }
            catch (Exception ex) { Plugin.Log.LogWarning("font fit: " + ex.Message); }
            _checkedText = text;
            _checkedFont = _label.font;
        }

        internal static void Fit(TMP_Text label)
        {
            var text = Visible(label.text);
            if (string.IsNullOrEmpty(text) || Covers(label.font, text)) return;

            if (!Cache.TryGetValue(text, out var font))
            {
                font = Find(text);
                Cache[text] = font;
                if (Logged.Add(text))
                    Plugin.Log.LogInfo("font fit: \"" + text + "\" -> " + (font != null ? font.name : "<no font has these characters>"));
            }
            if (font == null || font == label.font) return;
            label.font = font;
            label.fontSharedMaterial = font.material;
        }

        // Только печатаемые символы, без тегов разметки TMP (<sprite …>, <color …>).
        private static string Visible(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder(text.Length);
            bool inTag = false;
            foreach (var c in text)
            {
                if (c == '<') { inTag = true; continue; }
                if (c == '>' && inTag) { inTag = false; continue; }
                if (inTag || char.IsWhiteSpace(c) || char.IsControl(c)) continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool Covers(TMP_FontAsset font, string text)
        {
            if (font == null) return false;
            try { return font.HasCharacters(text, out uint[] _, true, true); }
            catch { return false; }
        }

        // Сначала шрифты видимых игровых надписей (их игра уже подобрала под язык), потом все
        // загруженные шрифты.
        private static TMP_FontAsset Find(string text)
        {
            var tried = new HashSet<TMP_FontAsset>();
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (t == null || t.font == null || !t.isActiveAndEnabled || !tried.Add(t.font)) continue;
                if (Covers(t.font, text)) return t.font;
            }
            foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (f == null || !tried.Add(f)) continue;
                if (Covers(f, text)) return f;
            }
            // Шрифты других языков игра грузит только для своего языка, но у каждого её шрифта
            // (LazyFontData) есть список замен по языкам (ko, ja, zh…) — загружаем их сами.
            foreach (var f in LanguageFonts())
            {
                if (f == null || !tried.Add(f)) continue;
                if (Covers(f, text)) return f;
            }
            return null;
        }

        private static readonly System.Reflection.FieldInfo OverridesField =
            HarmonyLib.AccessTools.Field(typeof(LazyBearTechnology.LazyFontData), "assetOverrideByLang");

        private static IEnumerable<TMP_FontAsset> LanguageFonts()
        {
            var result = new List<TMP_FontAsset>();
            try
            {
                foreach (var data in Resources.FindObjectsOfTypeAll<LazyBearTechnology.LazyFontData>())
                {
                    if (data == null || OverridesField == null) continue;
                    if (!(OverridesField.GetValue(data) is System.Collections.IList list)) continue;
                    foreach (var entry in list)
                    {
                        var langField = entry != null ? HarmonyLib.AccessTools.Field(entry.GetType(), "langId") : null;
                        var lang = langField != null ? langField.GetValue(entry) as string : null;
                        if (string.IsNullOrEmpty(lang)) continue;
                        try { var f = data.GetFontAssetFor(lang, false, false); if (f != null) result.Add(f); }
                        catch { }
                    }
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning("font fit: language fonts: " + ex.Message); }
            return result;
        }
    }
}
