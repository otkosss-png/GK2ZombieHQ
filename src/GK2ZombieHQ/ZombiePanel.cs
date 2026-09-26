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
        private ScrollRect _scroll;

        // Геймпад: свой список фокусируемых кнопок (legacy Input надёжнее GameKey-API).
        private readonly List<Button> _focusables = new List<Button>();
        private readonly Dictionary<Button, Color> _baseColors = new Dictionary<Button, Color>();
        private readonly Dictionary<Button, GameObject> _frames = new Dictionary<Button, GameObject>();
        private int _focusIdx = -1;
        private float _navCooldown;

        internal bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            Instance = this;
        }

        internal void Close()
        {
            if (_root == null || !_root.activeSelf) return;
            _root.SetActive(false);
            SetPaused(false);
        }

        // Пауза игры, пока открыта панель (иначе персонаж ходит по стрелкам).
        private static void SetPaused(bool paused)
        {
            try
            {
                if (paused) MainGame.PauseGame();
                else MainGame.UnpauseGame();
            }
            catch (Exception ex) { Plugin.Log.LogWarning("pause: " + ex.Message); }
        }

        // Геймпад: открыть панель по индексу кнопки (Keys.PanelGamepad).
        internal static bool TryGamepadOpen()
        {
            try
            {
                if (Plugin.Mod == null || Plugin.Mod.PanelGamepad == null) return false;
                int idx = Plugin.Mod.PanelGamepad.Value;
                if (idx < 0 || idx > 19) return false;
                return Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + idx));
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

            var overlay = UiFactory.PanelImage("Overlay", _root.transform, GameStyle.Dim);
            Stretch(overlay.rectTransform);

            // Сплошной фон панели: спрайт-«шапка» не заливал весь прямоугольник,
            // из-за чего нижние строки выглядели «за пределами» меню.
            var panel = UiFactory.PanelImage("Panel", overlay.transform, new Color(0.12f, 0.10f, 0.09f, 0.985f));
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
            RegisterButton(close);

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
            scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll = scroll;
        }

        // Прокручивает список так, чтобы выбранная кнопка была видна.
        private void EnsureVisible(RectTransform item)
        {
            if (_scroll == null || _scroll.viewport == null || _scroll.content == null || item == null) return;
            try
            {
                var vp = _scroll.viewport;
                var content = _scroll.content;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                float vpH = vp.rect.height;
                float contentH = Mathf.Max(content.rect.height, LayoutUtility.GetPreferredHeight(content));
                float maxScroll = Mathf.Max(0f, contentH - vpH);
                if (maxScroll <= 0.01f) return;

                var b = RectTransformUtility.CalculateRelativeRectTransformBounds(vp, item);
                float itemMin = b.min.y - vp.rect.yMin;
                float itemMax = b.max.y - vp.rect.yMin;

                float scroll = content.anchoredPosition.y; // 0 (верх) .. maxScroll (низ)
                if (itemMax > vpH) scroll += itemMax - vpH;
                else if (itemMin < 0f) scroll += itemMin;
                scroll = Mathf.Clamp(scroll, 0f, maxScroll);
                Plugin.Log.LogInfo($"scroll: vpH={vpH:0} contentH={contentH:0} max={maxScroll:0} itemMin={itemMin:0} itemMax={itemMax:0} scroll={scroll:0}");
                // ScrollRect сам выставляет позицию контента по нормализованному значению.
                _scroll.verticalNormalizedPosition = 1f - scroll / maxScroll;
            }
            catch { }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void RegisterButton(Button btn)
        {
            _focusables.Add(btn);
            var img = btn.targetGraphic as Image;
            _baseColors[btn] = img != null ? img.color : Color.white;
            _frames[btn] = CreateFocusFrame((RectTransform)btn.transform);
        }

        // Рамка вокруг выбранного элемента — как у игровых кнопок с геймпадом.
        private static GameObject CreateFocusFrame(RectTransform parent)
        {
            var frame = new GameObject("FocusFrame", typeof(RectTransform));
            frame.transform.SetParent(parent, false);
            var frt = (RectTransform)frame.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(-5, -5); frt.offsetMax = new Vector2(5, 5);
            Bar(frt, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 3), Vector2.zero);
            Bar(frt, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 3), Vector2.zero);
            Bar(frt, "Left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(3, 0), Vector2.zero);
            Bar(frt, "Right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(3, 0), Vector2.zero);
            frame.SetActive(false);
            return frame;
        }

        private static void Bar(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = GameStyle.Accent;
            img.raycastTarget = false;
        }

        internal void Toggle()
        {
            if (_root == null) return;
            bool show = !_root.activeSelf;
            _root.SetActive(show);
            if (show) { Refresh(); SetPaused(true); }
            else SetPaused(false);
        }

        private void Update()
        {
            try
            {
                if (_root == null) return;
                if (Input.GetKeyDown(Plugin.Mod.PanelKey.Value.MainKey)) Toggle();
                if (!_root.activeSelf) return;

                DriveGamepad();
                DriveWheel();
            }
            catch (Exception ex) { Plugin.Log.LogWarning("panel: " + ex.Message); }
        }

        // A (JoystickButton0) — выбрать, B (JoystickButton1) — закрыть, стрелки/D-pad/стик — фокус.
        private void DriveGamepad()
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) { SelectFocused(); return; }
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) { Close(); return; }

            // Клавиатурные стрелки.
            if (Input.GetKeyDown(KeyCode.UpArrow)) { MoveDirection(Vector2.up); return; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { MoveDirection(Vector2.down); return; }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { MoveDirection(Vector2.left); return; }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { MoveDirection(Vector2.right); return; }

            // Стик/D-pad.
            Vector2 dir = Vector2.zero;
            try { dir = LazyInput.GetDirection(); } catch { }
            if (dir.sqrMagnitude < 0.25f)
            {
                try { dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); } catch { }
            }

            _navCooldown -= Time.unscaledDeltaTime;
            if (_navCooldown > 0f || dir.sqrMagnitude < 0.25f) return;

            if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x)) MoveDirection(dir.y > 0f ? Vector2.up : Vector2.down);
            else MoveDirection(dir.x > 0f ? Vector2.right : Vector2.left);
            _navCooldown = 0.22f;
        }

        // Прокрутка колесом мыши (не полагаемся на EventSystem игры).
        private void DriveWheel()
        {
            if (_scroll == null || _scroll.content == null || _scroll.viewport == null) return;
            float wheel = 0f;
            try { wheel = Input.mouseScrollDelta.y; } catch { }
            if (Mathf.Abs(wheel) < 0.001f) return;

            var content = _scroll.content;
            float maxScroll = Mathf.Max(0f, content.rect.height - _scroll.viewport.rect.height);
            float scroll = Mathf.Clamp(content.anchoredPosition.y - wheel * 90f, 0f, maxScroll);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, scroll);
        }

        // Позиционная навигация: ближайшая кнопка в заданном направлении (с учётом выравнивания).
        private void MoveDirection(Vector2 dir)
        {
            if (_focusables.Count == 0) return;
            if (_focusIdx < 0) { FocusButton(0); return; }

            RectTransform cur = _focusables[_focusIdx].transform as RectTransform;
            if (cur == null) return;
            Vector2 curPos = cur.position;
            Vector2 cross = new Vector2(-dir.y, dir.x);

            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < _focusables.Count; i++)
            {
                if (i == _focusIdx) continue;
                var rt = _focusables[i].transform as RectTransform;
                if (rt == null) continue;
                Vector2 d = (Vector2)rt.position - curPos;
                float along = Vector2.Dot(d, dir);
                if (along <= 1f) continue;                       // не в этом направлении
                float off = Mathf.Abs(Vector2.Dot(d, cross));
                float score = along + off * 3f;                  // ближе и ровнее — лучше
                if (score < bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0) FocusButton(best);
        }

        private void FocusButton(int idx)
        {
            _focusIdx = idx;
            for (int i = 0; i < _focusables.Count; i++)
            {
                bool focused = i == idx;
                var img = _focusables[i].targetGraphic as Image;
                if (img != null)
                {
                    Color baseColor;
                    if (!_baseColors.TryGetValue(_focusables[i], out baseColor)) baseColor = Color.white;
                    img.color = focused ? GameStyle.Accent : baseColor;
                }
                GameObject frame;
                if (_frames.TryGetValue(_focusables[i], out frame) && frame != null) frame.SetActive(focused);
            }
            if (idx >= 0 && idx < _focusables.Count)
                EnsureVisible(_focusables[idx].transform as RectTransform);
        }

        private void SelectFocused()
        {
            if (_focusIdx < 0 || _focusIdx >= _focusables.Count) return;
            _focusables[_focusIdx].onClick.Invoke();
        }

        private void Refresh()
        {
            int count = ZombieRoster.Count();
            int max = Plugin.Mod.HudMaxZombies.Value;
            _countLabel.text = count < 0 ? "" : HudFormat.Count(ZombieText.Language, count, max);

            foreach (Transform child in _content) Destroy(child.gameObject);
            _focusables.Clear();
            _baseColors.Clear();
            _frames.Clear();

            var entries = ZombieRoster.Load();
            if (entries.Count == 0)
            {
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 24, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                _focusIdx = -1;
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
                RegisterButton(openBtn);

                var camBtn = UiFactory.TextButton("Camera", row, ZombieText.Get("Camera"), 30);
                var crt2 = camBtn.GetComponent<RectTransform>();
                crt2.anchorMin = new Vector2(1, 0.5f); crt2.anchorMax = new Vector2(1, 0.5f);
                crt2.pivot = new Vector2(1, 0.5f); crt2.sizeDelta = new Vector2(200, 60);
                crt2.anchoredPosition = new Vector2(-236, 0);
                camBtn.onClick.AddListener(() => { ZombieRoster.FocusCamera(entry); Close(); });
                RegisterButton(camBtn);

                if (e.Info.CanRecall)
                {
                    var rec = UiFactory.TextButton("Recall", row, ZombieText.Get("Recall"), 30);
                    var rrt = rec.GetComponent<RectTransform>();
                    rrt.anchorMin = new Vector2(1, 0.5f); rrt.anchorMax = new Vector2(1, 0.5f);
                    rrt.pivot = new Vector2(1, 0.5f); rrt.sizeDelta = new Vector2(200, 60);
                    rrt.anchoredPosition = new Vector2(-448, 0);
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                    RegisterButton(rec);
                }
            }

            // Сохраняем/восстанавливаем фокус, чтобы подсветка не пропадала.
            if (_focusables.Count > 0) FocusButton(Mathf.Clamp(_focusIdx, 0, _focusables.Count - 1));
        }
    }
}
