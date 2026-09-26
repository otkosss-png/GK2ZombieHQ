using System;
using System.Collections.Generic;
using GK2ZombieHQ.Core;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal sealed class ZombiePanel : MonoBehaviour
    {
        internal static ZombiePanel Instance;

        private GameObject _root;
        private RectTransform _content;
        private TextMeshProUGUI _countLabel;
        private float _timer;
        private GamepadNavigationController _nav;
        private readonly List<GamepadNavigationItem> _navItems = new List<GamepadNavigationItem>();

        internal bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            Instance = this;
        }

        internal void Close()
        {
            if (_root == null) return;
            _root.SetActive(false);
            try { if (_nav != null) _nav.Disable(); } catch { }
        }

        // Геймпад: открытие панели по настраиваемой GameKey (см. Keys.PanelGamepad).
        internal static bool TryGamepadOpen()
        {
            try
            {
                if (Plugin.Mod == null) return false;
                var name = Plugin.Mod.PanelGamepad.Value;
                if (string.IsNullOrEmpty(name)) return false;
                // GameKey — не enum, а класс со статическими полями; ищем по имени.
                var field = typeof(GameKey).GetField(name,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.IgnoreCase);
                var key = field != null ? field.GetValue(null) as GameKey : null;
                if (key == null || key == GameKey.None) return false;
                return LazyInput.GetKeyDown(key);
            }
            catch { return false; }
        }

        private void Start()
        {
            try
            {
                BuildUi();
                _root.SetActive(false);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("panel build: " + ex); }
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
            _nav = _root.AddComponent<GamepadNavigationController>();

            var overlay = UiFactory.PanelImage("Overlay", _root.transform, GameStyle.Dim);
            Stretch(overlay.rectTransform);

            var panel = UiFactory.PanelImage("Panel", overlay.transform, GameStyle.PanelBg, GameStyle.PanelSprite);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.05f, 0.09f);
            prt.anchorMax = new Vector2(0.95f, 0.91f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var title = UiFactory.Label("Title", prt, ZombieText.Get("Title"), 52, TextAlignmentOptions.Left, GameStyle.Accent);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(24, -16);
            trt.sizeDelta = new Vector2(-200, 48);

            var close = UiFactory.TextButton("Close", prt, ZombieText.Get("Close"), 30);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-16, -16);
            crt.sizeDelta = new Vector2(200, 60);
            close.onClick.AddListener(Close);
            RegisterNav(close);

            _countLabel = UiFactory.Label("Count", prt, "", 44, TextAlignmentOptions.Left);
            var cnt = _countLabel.rectTransform;
            cnt.anchorMin = new Vector2(0, 1); cnt.anchorMax = new Vector2(1, 1);
            cnt.pivot = new Vector2(0.5f, 1); cnt.anchoredPosition = new Vector2(24, -92);
            cnt.sizeDelta = new Vector2(-48, 50);

            var viewport = UiFactory.PanelImage("Viewport", prt, new Color(0, 0, 0, 0.18f));
            var vrt = viewport.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(20, 20); vrt.offsetMax = new Vector2(-20, -158);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();

            _content = UiFactory.Rect("Content", vrt);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1); _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = Vector2.zero;
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

        private GamepadNavigationItem RegisterNav(Button btn)
        {
            var item = btn.gameObject.AddComponent<GamepadNavigationItem>();
            var img = btn.targetGraphic as Image;
            item.OnSelect.AddListener(() => btn.onClick.Invoke());
            if (img != null)
            {
                var baseColor = img.color;
                item.OnFocus.AddListener(() => img.color = GameStyle.Accent);
                item.OnUnfocus.AddListener(() => img.color = baseColor);
            }
            _navItems.Add(item);
            return item;
        }

        internal void Toggle()
        {
            if (_root == null) return;
            bool show = !_root.activeSelf;
            _root.SetActive(show);
            if (show) Refresh();
            else { try { if (_nav != null) _nav.Disable(); } catch { } }
        }

        private void Update()
        {
            try
            {
                if (_root == null) return;
                if (Plugin.Mod != null && Plugin.Mod.PanelKey != null && Input.GetKeyDown(Plugin.Mod.PanelKey.Value.MainKey)) Toggle();
                if (!_root.activeSelf) return;

                DriveGamepad();

                _timer -= Time.unscaledDeltaTime;
                if (_timer <= 0f) { _timer = 0.75f; Refresh(); }
            }
            catch (Exception ex) { Plugin.Log.LogWarning("panel: " + ex.Message); }
        }

        // Игра не опрашивает GamepadNavigationController сама — ведём навигацию из LazyInput.
        private void DriveGamepad()
        {
            if (_nav == null || !_nav.IsEnabled) return;
            if (LazyInput.GetKeyDown(GameKey.Up)) _nav.Navigate(GUIDirection.Up);
            else if (LazyInput.GetKeyDown(GameKey.Down)) _nav.Navigate(GUIDirection.Down);
            else if (LazyInput.GetKeyDown(GameKey.Left)) _nav.Navigate(GUIDirection.Left);
            else if (LazyInput.GetKeyDown(GameKey.Right)) _nav.Navigate(GUIDirection.Right);
            else if (LazyInput.GetKeyDown(GameKey.Interaction)) _nav.SelectFocusedItem();
        }

        private void Refresh()
        {
            int count = ZombieRoster.Count();
            int max = Plugin.Mod.HudMaxZombies.Value;
            _countLabel.text = count < 0 ? "" : HudFormat.Count(ZombieText.Language, count, max);

            foreach (Transform child in _content) Destroy(child.gameObject);
            _navItems.Clear();

            var entries = ZombieRoster.Load();
            if (entries.Count == 0)
            {
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 24, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                ReinitNav();
                return;
            }

            foreach (var e in entries)
            {
                var row = UiFactory.Rect("Row", _content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 74;

                string label = e.Info.Name + "  ·  " + ZombieText.KindName(e.Info.Kind)
                    + "  ·  " + RosterLogic.Skulls(e.Info) + (e.Info.Collar != null ? "  ·  " + e.Info.Collar : "");
                var name = UiFactory.Label("Name", row, label, 34, TextAlignmentOptions.Left);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
                var nrt = name.rectTransform;
                nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(1, 1);
                nrt.offsetMin = new Vector2(16, 6); nrt.offsetMax = new Vector2(-672, -6);

                var entry = e;

                var openBtn = UiFactory.TextButton("Open", row, ZombieText.Get("Open"), 30);
                var ort = openBtn.GetComponent<RectTransform>();
                ort.anchorMin = new Vector2(1, 0.5f); ort.anchorMax = new Vector2(1, 0.5f);
                ort.pivot = new Vector2(1, 0.5f); ort.sizeDelta = new Vector2(200, 60);
                ort.anchoredPosition = new Vector2(-24, 0);
                openBtn.onClick.AddListener(() => { ZombieRoster.OpenWindow(entry); Close(); });
                RegisterNav(openBtn);

                var camBtn = UiFactory.TextButton("Camera", row, ZombieText.Get("Camera"), 30);
                var crt2 = camBtn.GetComponent<RectTransform>();
                crt2.anchorMin = new Vector2(1, 0.5f); crt2.anchorMax = new Vector2(1, 0.5f);
                crt2.pivot = new Vector2(1, 0.5f); crt2.sizeDelta = new Vector2(200, 60);
                crt2.anchoredPosition = new Vector2(-236, 0);
                camBtn.onClick.AddListener(() => { ZombieRoster.FocusCamera(entry); Close(); });
                RegisterNav(camBtn);

                if (e.Info.CanRecall)
                {
                    var rec = UiFactory.TextButton("Recall", row, ZombieText.Get("Recall"), 30);
                    var rrt = rec.GetComponent<RectTransform>();
                    rrt.anchorMin = new Vector2(1, 0.5f); rrt.anchorMax = new Vector2(1, 0.5f);
                    rrt.pivot = new Vector2(1, 0.5f); rrt.sizeDelta = new Vector2(200, 60);
                    rrt.anchoredPosition = new Vector2(-448, 0);
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                    RegisterNav(rec);
                }
            }

            ReinitNav();
        }

        private void ReinitNav()
        {
            try
            {
                if (_nav == null) return;
                _nav.ReinitItems(true, _navItems, null);
                _nav.Enable(true);
            }
            catch (Exception ex) { Plugin.Log.LogWarning("nav: " + ex.Message); }
        }
    }
}
