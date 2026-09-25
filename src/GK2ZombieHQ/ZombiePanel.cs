using GK2ZombieHQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal sealed class ZombiePanel : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _content;
        private TextMeshProUGUI _countLabel;
        private float _timer;

        private void Start()
        {
            try
            {
                BuildUi();
                _root.SetActive(false);
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("panel build: " + ex); }
        }

        private void BuildUi()
        {
            _root = new GameObject("GK2ZombieHQ_Panel");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5100;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            var overlay = UiFactory.PanelImage("Overlay", _root.transform, GameStyle.Dim);
            Stretch(overlay.rectTransform);

            var panel = UiFactory.PanelImage("Panel", overlay.transform, GameStyle.PanelBg, GameStyle.PanelSprite);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.05f, 0.09f);
            prt.anchorMax = new Vector2(0.95f, 0.91f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var title = UiFactory.Label("Title", prt, ZombieText.Get("Title"), 36, TextAlignmentOptions.Left, GameStyle.Accent);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(24, -16);
            trt.sizeDelta = new Vector2(-200, 48);

            var close = UiFactory.TextButton("Close", prt, ZombieText.Get("Close"), 24);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-16, -16);
            crt.sizeDelta = new Vector2(150, 46);
            close.onClick.AddListener(() => _root.SetActive(false));

            _countLabel = UiFactory.Label("Count", prt, "", 30, TextAlignmentOptions.Left);
            var cnt = _countLabel.rectTransform;
            cnt.anchorMin = new Vector2(0, 1); cnt.anchorMax = new Vector2(1, 1);
            cnt.pivot = new Vector2(0.5f, 1); cnt.anchoredPosition = new Vector2(24, -74);
            cnt.sizeDelta = new Vector2(-48, 36);

            var viewport = UiFactory.PanelImage("Viewport", prt, new Color(0, 0, 0, 0.18f));
            var vrt = viewport.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(20, 20); vrt.offsetMax = new Vector2(-20, -120);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();

            _content = UiFactory.Rect("Content", vrt);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1); _content.anchoredPosition = Vector2.zero;
            var vlg = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 6; vlg.padding = new RectOffset(6, 6, 6, 6);
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
            scroll.viewport = vrt;
            scroll.horizontal = false;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        internal void Toggle()
        {
            if (_root == null) return;
            bool show = !_root.activeSelf;
            _root.SetActive(show);
            if (show) Refresh();
        }

        private void Update()
        {
            try
            {
                if (_root == null) return;
                if (Input.GetKeyDown(Plugin.Mod.PanelKey.Value.MainKey)) Toggle();
                if (!_root.activeSelf) return;
                _timer -= Time.unscaledDeltaTime;
                if (_timer <= 0f) { _timer = 0.75f; Refresh(); }
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("panel: " + ex.Message); }
        }

        private void Refresh()
        {
            int count = ZombieRoster.Count();
            _countLabel.text = count < 0 ? "" : HudFormat.Count(ZombieText.Language, count, 0);

            foreach (Transform child in _content) Destroy(child.gameObject);
            var entries = ZombieRoster.Load();
            if (entries.Count == 0)
            {
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 24, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                return;
            }

            foreach (var e in entries)
            {
                var row = UiFactory.Rect("Row", _content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;

                string label = e.Info.Name + "  ·  " + ZombieText.KindName(e.Info.Kind)
                    + "  ·  " + RosterLogic.Skulls(e.Info) + (e.Info.Collar != null ? "  ·  " + e.Info.Collar : "");
                var name = UiFactory.Label("Name", row, label, 24, TextAlignmentOptions.Left);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
                var nrt = name.rectTransform;
                nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(1, 1);
                nrt.offsetMin = new Vector2(14, 6); nrt.offsetMax = new Vector2(-338, -6);

                var entry = e;

                // Кнопки фиксированной ширины у правого края — не выходят за пределы строки.
                var openBtn = UiFactory.TextButton("Open", row, ZombieText.Get("Open"), 22);
                var ort = openBtn.GetComponent<RectTransform>();
                ort.anchorMin = new Vector2(1, 0.5f); ort.anchorMax = new Vector2(1, 0.5f);
                ort.pivot = new Vector2(1, 0.5f); ort.sizeDelta = new Vector2(150, 42);
                ort.anchoredPosition = new Vector2(-10, 0);
                openBtn.onClick.AddListener(() => { ZombieRoster.OpenWindow(entry); _root.SetActive(false); });

                if (e.Info.CanRecall)
                {
                    var rec = UiFactory.TextButton("Recall", row, ZombieText.Get("Recall"), 22);
                    var rrt = rec.GetComponent<RectTransform>();
                    rrt.anchorMin = new Vector2(1, 0.5f); rrt.anchorMax = new Vector2(1, 0.5f);
                    rrt.pivot = new Vector2(1, 0.5f); rrt.sizeDelta = new Vector2(150, 42);
                    rrt.anchoredPosition = new Vector2(-172, 0);
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                }
            }
        }
    }
}
