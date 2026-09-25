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

            // Панель всегда внутри экрана: якоря по краям, а не фиксированный размер.
            var panel = UiFactory.PanelImage("Panel", overlay.transform, GameStyle.PanelBg, GameStyle.PanelSprite);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.06f, 0.10f);
            prt.anchorMax = new Vector2(0.94f, 0.90f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var title = UiFactory.Label("Title", prt, ZombieText.Get("Title"), 30, TextAlignmentOptions.Left, GameStyle.Accent);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(20, -14);
            trt.sizeDelta = new Vector2(-160, 40);

            var close = UiFactory.TextButton("Close", prt, ZombieText.Get("Close"), 20);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-14, -14);
            crt.sizeDelta = new Vector2(120, 40);
            close.onClick.AddListener(() => _root.SetActive(false));

            _countLabel = UiFactory.Label("Count", prt, "", 24, TextAlignmentOptions.Left);
            var cnt = _countLabel.rectTransform;
            cnt.anchorMin = new Vector2(0, 1); cnt.anchorMax = new Vector2(1, 1);
            cnt.pivot = new Vector2(0.5f, 1); cnt.anchoredPosition = new Vector2(20, -60);
            cnt.sizeDelta = new Vector2(-40, 30);

            var viewport = UiFactory.PanelImage("Viewport", overlay.transform, new Color(0, 0, 0, 0.18f));
            var vrt = viewport.rectTransform;
            vrt.SetParent(prt, false);
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(16, 16); vrt.offsetMax = new Vector2(-16, -100);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();

            _content = UiFactory.Rect("Content", vrt);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1); _content.anchoredPosition = Vector2.zero;
            var vlg = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 6; vlg.padding = new RectOffset(4, 4, 4, 4);
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

        private void Update()
        {
            try
            {
                if (_root == null) return;
                if (Input.GetKeyDown(Plugin.Mod.PanelKey.Value.MainKey))
                {
                    bool show = !_root.activeSelf;
                    _root.SetActive(show);
                    if (show) Refresh();
                }
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
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 20, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
                return;
            }

            foreach (var e in entries)
            {
                var row = UiFactory.Rect("Row", _content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 46;
                var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlHeight = true; hlg.childControlWidth = true;
                hlg.childForceExpandHeight = false; hlg.childForceExpandWidth = false;
                hlg.spacing = 8;
                hlg.padding = new RectOffset(8, 8, 4, 4);

                string label = e.Info.Name + "  ·  " + ZombieText.KindName(e.Info.Kind)
                    + "  ·  " + RosterLogic.Skulls(e.Info) + (e.Info.Collar != null ? "  ·  " + e.Info.Collar : "");
                var name = UiFactory.Label("Name", row, label, 20, TextAlignmentOptions.Left);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
                var nameLe = name.gameObject.AddComponent<LayoutElement>();
                nameLe.flexibleWidth = 1;
                nameLe.minWidth = 0;

                var openBtn = UiFactory.TextButton("Open", row, ZombieText.Get("Open"), 18);
                openBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;
                var entry = e;
                openBtn.onClick.AddListener(() => { ZombieRoster.OpenWindow(entry); _root.SetActive(false); });

                if (e.Info.CanRecall)
                {
                    var rec = UiFactory.TextButton("Recall", row, ZombieText.Get("Recall"), 18);
                    rec.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                }
            }
        }
    }
}
