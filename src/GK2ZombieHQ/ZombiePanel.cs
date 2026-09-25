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
            _root.AddComponent<CanvasScaler>();
            _root.AddComponent<GraphicRaycaster>();

            var overlay = UiFactory.PanelImage("Overlay", _root.transform, new Color(0f, 0f, 0f, 0.6f));
            Stretch(overlay.rectTransform);

            var panel = UiFactory.PanelImage("Panel", overlay.transform, new Color(0.10f, 0.10f, 0.12f, 0.98f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(960, 600);
            prt.anchoredPosition = Vector2.zero;

            var title = UiFactory.Label("Title", prt, ZombieText.Get("Title"), 26, TextAlignmentOptions.Left);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(0, -8);
            trt.sizeDelta = new Vector2(-24, 36);

            var close = UiFactory.TextButton("Close", prt, ZombieText.Get("Close"), 18);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-8, -8);
            crt.sizeDelta = new Vector2(96, 32);
            close.onClick.AddListener(() => _root.SetActive(false));

            _countLabel = UiFactory.Label("Count", prt, "", 20, TextAlignmentOptions.Left);
            var cnt = _countLabel.rectTransform;
            cnt.anchorMin = new Vector2(0, 1); cnt.anchorMax = new Vector2(1, 1);
            cnt.pivot = new Vector2(0.5f, 1); cnt.anchoredPosition = new Vector2(0, -48);
            cnt.sizeDelta = new Vector2(-24, 28);

            var viewport = UiFactory.PanelImage("Viewport", prt, new Color(0, 0, 0, 0.25f));
            var vrt = viewport.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(12, 12); vrt.offsetMax = new Vector2(-12, -84);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vlgRoot = viewport.gameObject.AddComponent<ScrollRect>();
            var scroll = vlgRoot;

            _content = UiFactory.Rect("Content", vrt);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1); _content.anchoredPosition = Vector2.zero;
            var vlg = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 4; vlg.padding = new RectOffset(6, 6, 6, 6);
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
                if (Input.GetKeyDown(Plugin.Instance.PanelKey.Value))
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
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 18, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;
                return;
            }

            foreach (var e in entries)
            {
                var row = UiFactory.Rect("Row", _content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlHeight = true; hlg.childControlWidth = true;
                hlg.childForceExpandWidth = false; hlg.spacing = 6;

                string label = e.Info.Name + "  ·  " + ZombieText.KindName(e.Info.Kind)
                    + "  ·  " + RosterLogic.Skulls(e.Info) + (e.Info.Collar != null ? "  ·  " + e.Info.Collar : "");
                var name = UiFactory.Label("Name", row, label, 18, TextAlignmentOptions.Left);
                name.enableWordWrapping = false;
                name.overflowMode = TextOverflowModes.Ellipsis;
                var nameLe = name.gameObject.AddComponent<LayoutElement>();
                nameLe.flexibleWidth = 1;
                nameLe.minWidth = 0;

                var openBtn = UiFactory.TextButton("Open", row, ZombieText.Get("Open"), 16);
                openBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;
                var entry = e;
                openBtn.onClick.AddListener(() => { ZombieRoster.OpenWindow(entry); _root.SetActive(false); });

                if (e.Info.CanRecall)
                {
                    var rec = UiFactory.TextButton("Recall", row, ZombieText.Get("Recall"), 16);
                    rec.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                }
            }
        }
    }
}
