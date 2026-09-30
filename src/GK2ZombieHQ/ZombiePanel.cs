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
        private Button _closeButton;

        // Геймпад: свой список фокусируемых кнопок (legacy Input надёжнее GameKey-API).
        private readonly List<Button> _focusables = new List<Button>();
        private readonly Dictionary<Button, Color> _baseColors = new Dictionary<Button, Color>();
        private readonly Dictionary<Button, GameObject> _frames = new Dictionary<Button, GameObject>();
        private int _focusIdx = -1;
        private float _navCooldown;

        internal bool IsOpen => _root != null && _root.activeSelf;

        // «Камера»/«Открыть» уводят из панели; когда игрок выйдет из камеры или закроет окно
        // зомби — возвращаем панель на ту же прокрутку и ту же кнопку.
        private enum ReturnFrom { None, Camera, ZombieWindow }
        private ReturnFrom _returnFrom;
        private float _savedScroll;
        private int _savedFocus = -1;
        private int _reopenedFrame = -1;
        private float _suspendedAt;

        // Кадр возврата панели: то же нажатие Esc/B, что закрыло камеру/окно, не должно закрыть её.
        internal bool JustReopened => Time.frameCount == _reopenedFrame;

        private void SuspendFor(ReturnFrom from)
        {
            _savedScroll = _scroll != null && _scroll.content != null ? _scroll.content.anchoredPosition.y : 0f;
            _savedFocus = _focusIdx;
            _returnFrom = from;
            _suspendedAt = Time.unscaledTime;
            Close();
        }

        // Вернуть панель, когда камера выключена / окно зомби закрыто.
        private void TryReturn()
        {
            if (_returnFrom == ReturnFrom.None || _root == null || _root.activeSelf) return;
            if (!ZombieRoster.GameReady()) { _returnFrom = ReturnFrom.None; return; }
            // Окну игры нужен кадр-другой, чтобы открыться: не принимаем «ещё не открылось» за «закрыли».
            if (Time.unscaledTime - _suspendedAt < 0.5f) return;
            if (_returnFrom == ReturnFrom.Camera)
            {
                var cam = ZombieCameraFollow.Instance;
                if (cam != null && cam.IsActive) return;
            }
            else if (_returnFrom == ReturnFrom.ZombieWindow)
            {
                bool shown = false;
                try { shown = LazyUI.GetWindow<UIZombieWorkerWindow>().IsShown; } catch { }
                if (shown) return;
            }

            _returnFrom = ReturnFrom.None;
            _root.SetActive(true);
            _focusIdx = _savedFocus;
            Refresh();
            SetPaused(true);
            RestoreScroll(_savedScroll);
            _reopenedFrame = Time.frameCount;
        }

        private void RestoreScroll(float y)
        {
            if (_scroll == null || _scroll.content == null || _scroll.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            var content = _scroll.content;
            float maxScroll = Mathf.Max(0f, content.rect.height - _scroll.viewport.rect.height);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(y, 0f, maxScroll));
        }

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
                var mg = MainGame.Instance;
                if (mg == null) return;
                var mi = typeof(MainGame).GetMethod(paused ? "PauseGame" : "UnpauseGame",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (mi != null) mi.Invoke(mg, null);
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
            _closeButton = close;

            _countLabel = UiFactory.Label("Count", prt, "", 44, TextAlignmentOptions.Left);
            var cnt = _countLabel.rectTransform;
            cnt.anchorMin = new Vector2(0, 1); cnt.anchorMax = new Vector2(1, 1);
            cnt.pivot = new Vector2(0.5f, 1); cnt.anchoredPosition = new Vector2(24, -92);
            cnt.sizeDelta = new Vector2(-48, 50);

            // Подсказка снаряжения: наведение на значок пишет текст сюда (см. GearTipHover).
            _gearTip = UiFactory.Label("GearTip", prt, "", 26, TextAlignmentOptions.Left, GameStyle.Accent);
            var grt = _gearTip.rectTransform;
            grt.anchorMin = new Vector2(0, 1); grt.anchorMax = new Vector2(1, 1);
            grt.pivot = new Vector2(0.5f, 1); grt.anchoredPosition = new Vector2(24, -136);
            grt.sizeDelta = new Vector2(-48, 30);
            _gearTip.textWrappingMode = TextWrappingModes.NoWrap;
            _gearTip.overflowMode = TextOverflowModes.Ellipsis;

            var viewport = UiFactory.PanelImage("Viewport", prt, new Color(0, 0, 0, 0.18f));
            var vrt = viewport.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(20, 20); vrt.offsetMax = new Vector2(-20, -180);
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

                // Позиция кнопки относительно контента (мировые углы → локальные контента).
                var corners = new Vector3[4];
                item.GetWorldCorners(corners);
                float itemTop = content.InverseTransformPoint(corners[1]).y;
                float itemBottom = content.InverseTransformPoint(corners[0]).y;
                float contentTop = content.rect.yMax;
                float topOffset = contentTop - itemTop;       // от верха контента до верха кнопки
                float bottomOffset = contentTop - itemBottom; // от верха контента до низа кнопки

                float s = maxScroll * (1f - _scroll.verticalNormalizedPosition); // 0 (верх) .. max (низ)
                if (topOffset - s < 0f) s = topOffset;                        // кнопка выше вида
                else if (bottomOffset - s > vpH) s = bottomOffset - vpH;      // кнопка ниже вида
                s = Mathf.Clamp(s, 0f, maxScroll);
                _scroll.verticalNormalizedPosition = 1f - s / maxScroll;
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
            if (!_root.activeSelf && !ZombieRoster.GameReady()) return; // в меню не открываем
            bool show = !_root.activeSelf;
            _returnFrom = ReturnFrom.None; // открыли/закрыли вручную — автовозврат больше не нужен
            _root.SetActive(show);
            if (show) { Refresh(); SetPaused(true); }
            else SetPaused(false);
        }

        private void Update()
        {
            try
            {
                if (_root == null) return;
                // Сейв выгружен (выход в меню) — закрываем панель и снимаем паузу.
                if (_root.activeSelf && !ZombieRoster.GameReady()) { _root.SetActive(false); SetPaused(false); return; }
                if (Input.GetKeyDown(Plugin.Mod.PanelKey.Value.MainKey)) Toggle();
                TryReturn();
                if (!_root.activeSelf || JustReopened) return;

                DriveGamepad();
                DriveWheel();
                UpdateWorkRows();
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

        // Иконка черепа (белый/красный глиф TMP) + число.
        // Спрайт-ассет с глифами кристаллов технологий (tech_blue/tech_green/tech_red).
        private static TMP_SpriteAsset _techAsset;
        private static bool _techAssetResolved;

        private static TMP_SpriteAsset TechAsset()
        {
            if (_techAssetResolved) return _techAsset;
            _techAssetResolved = true;
            _techAsset = GameStyle.SpriteAssetFor("tech_blue");
            Plugin.Log.LogInfo("style: tech glyph asset = " + (_techAsset != null ? _techAsset.name : "none"));
            return _techAsset;
        }

        // Раскладка строки (слева направо): "N. Имя · Тип", белые черепа, красные "3/5",
        // снаряжение (ошейник/инструмент/броня), очки красные/зелёные/синие. Справа — кнопки.
        // Вторая строка: статус под именем, переносимые вещи носильщика под снаряжением.
        private const float RowHeight = 108f;
        private const float MainLineY = 8f;
        private const float SecondLineY = -36f;
        private const float TitleX = 16f, TitleWidth = 400f;
        private const float WhiteX = 425f, WhiteWidth = 85f;
        private const float RedX = 510f, RedWidth = 120f;
        private const float GearX = 635f;
        private const float TechX = 845f, TechWidth = 240f;
        private const float ButtonWidth = 180f, ButtonHeight = 60f, ButtonGap = 10f, ButtonRight = 16f;

        // Прямоугольник, привязанный к левому краю строки по центру по вертикали.
        private static RectTransform LeftRect(string name, RectTransform row, float x, float y, float w, float h)
        {
            var r = UiFactory.Rect(name, row);
            r.anchorMin = new Vector2(0, 0.5f); r.anchorMax = new Vector2(0, 0.5f);
            r.pivot = new Vector2(0, 0.5f);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = new Vector2(x, y);
            return r;
        }

        private static TextMeshProUGUI LeftLabel(string name, RectTransform row, string text, int size, float x, float y, float w, float h, Color? color = null)
        {
            var box = LeftRect(name, row, x, y, w, h);
            var label = color.HasValue
                ? UiFactory.Label("Text", box, text, size, TextAlignmentOptions.Left, color.Value)
                : UiFactory.Label("Text", box, text, size, TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return label;
        }

        // Кнопка строки справа налево: Открыть, Камера, Отозвать.
        private Button MakeRowButton(RectTransform row, string key, ref float right)
        {
            var btn = UiFactory.TextButton(key, row, ZombieText.Get(key), 28);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f); rt.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rt.anchoredPosition = new Vector2(-right, MainLineY);
            right += ButtonWidth + ButtonGap;
            RegisterButton(btn);
            return btn;
        }

        private static void MakeSkull(RectTransform row, float x, float width, string glyph, string value)
        {
            var label = LeftLabel("Skull", row, "", 30, x, MainLineY, width, 44);
            var sa = GameStyle.SpriteAsset;
            if (sa != null) label.spriteAsset = sa;
            label.text = (sa != null ? "<sprite name=\"" + glyph + "\"> " : "") + value;
        }

        // Снаряжение: ошейник/инструмент/броня (пустые — тусклая заглушка) на основной строке,
        // переносимые вещи носильщика — мельче, второй строкой. Наведение на значок
        // показывает название в подсказке панели.
        private const int EquipSlots = 3;
        // Значки снаряжения — высотой с кнопки строки.
        private const float GearIconSize = ButtonHeight, GearIconStep = ButtonHeight + 4f;
        private const float CarriedIconSize = 24f, CarriedIconStep = 28f;

        private void MakeGearStrip(RectTransform row, ZombieInfo info)
        {
            if (info == null || info.Gear == null || info.Gear.Count == 0) return;

            for (int i = 0; i < info.Gear.Count; i++)
            {
                var icon = info.Gear[i];
                bool equip = i < EquipSlots;
                float size = equip ? GearIconSize : CarriedIconSize;
                float x = equip ? GearX + i * GearIconStep : GearX + (i - EquipSlots) * CarriedIconStep;
                var go = LeftRect("Slot" + i, row, x, equip ? MainLineY : SecondLineY, size, size);

                var sprite = icon.IsEmpty ? null : GameStyle.ItemSprite(icon.Id, icon.IconId);
                var img = go.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                if (sprite != null) GameStyle.ApplyItemIconMaterial(img); // контур — как в игре, не синий
                img.raycastTarget = true;
                img.color = icon.IsEmpty
                    ? new Color(1f, 1f, 1f, 0.16f)          // пустой слот — тускло
                    : Color.white;
                if (sprite == null && !icon.IsEmpty) img.color = GameStyle.Accent;

                if (icon.Count > 1)
                {
                    var count = UiFactory.Label("Count", go, "x" + icon.Count, 18, TextAlignmentOptions.Right, GameStyle.Text);
                    var crt = count.rectTransform;
                    crt.anchorMin = new Vector2(1, 0); crt.anchorMax = new Vector2(1, 0);
                    crt.pivot = new Vector2(1, 0);
                    crt.sizeDelta = new Vector2(GearIconSize, 18);
                    crt.anchoredPosition = new Vector2(2, -2);
                    count.raycastTarget = false;
                }

                var hover = go.gameObject.AddComponent<GearTipHover>();
                hover.Tip = ZombieText.GearTip(icon);
                hover.Owner = this;
            }
        }

        // Строка работы: [иконка станции] Станция   [иконка предмета] Доски ×2 · 45%.
        // Занятие обновляется раз в секунду, пока панель открыта (UpdateWorkRows).
        private const float WorkIconSize = 30f;
        private const float StationX = TitleX, StationNameWidth = 240f;
        private const float JobX = 310f, JobWidth = 290f;

        private sealed class WorkRow
        {
            public RosterEntry Entry;
            public TextMeshProUGUI Job;
            public Image JobIcon;
            public RectTransform JobBox;
            public string IconKey;
        }

        private readonly List<WorkRow> _workRows = new List<WorkRow>();
        private float _workTimer;

        private void MakeWorkLine(RectTransform row, RosterEntry e)
        {
            var work = e.Info.Work;
            float x = StationX;
            if (e.StationIcon != null)
            {
                var iconRt = LeftRect("StationIcon", row, x, SecondLineY, WorkIconSize, WorkIconSize);
                var img = iconRt.gameObject.AddComponent<Image>();
                img.sprite = e.StationIcon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                x += WorkIconSize + 6f;
            }
            if (!string.IsNullOrEmpty(work.StationName))
                LeftLabel("Station", row, work.StationName, 22, x, SecondLineY, StationX + StationNameWidth + WorkIconSize - x, 28, GameStyle.Accent);

            if (work.Passive) return;
            var jobIconRt = LeftRect("JobIcon", row, JobX, SecondLineY, WorkIconSize, WorkIconSize);
            var jobIcon = jobIconRt.gameObject.AddComponent<Image>();
            jobIcon.preserveAspect = true;
            jobIcon.raycastTarget = false;
            var job = LeftLabel("Job", row, "", 22, JobX, SecondLineY, JobWidth, 28, GameStyle.Text);
            var wr = new WorkRow { Entry = e, Job = job, JobIcon = jobIcon, JobBox = (RectTransform)job.transform.parent };
            ApplyWork(wr);
            _workRows.Add(wr);
        }

        // Иконка предмета на выходе (если есть) и текст занятия; без иконки текст сдвигается влево.
        private static void ApplyWork(WorkRow wr)
        {
            var work = wr.Entry.Info.Work;
            string key = work.OutputId + "|" + work.OutputIconId;
            if (key != wr.IconKey)
            {
                wr.IconKey = key;
                var sprite = string.IsNullOrEmpty(work.OutputId) ? null : GameStyle.ItemSprite(work.OutputId, work.OutputIconId);
                wr.JobIcon.sprite = sprite;
                wr.JobIcon.gameObject.SetActive(sprite != null);
                if (sprite != null) GameStyle.ApplyItemIconMaterial(wr.JobIcon);
                float textX = sprite != null ? JobX + WorkIconSize + 6f : JobX;
                wr.JobBox.anchoredPosition = new Vector2(textX, SecondLineY);
                wr.JobBox.sizeDelta = new Vector2(JobX + JobWidth - textX, wr.JobBox.sizeDelta.y);
            }
            var text = WorkLogic.Job(work) ?? string.Empty;
            if (wr.Job.text != text) wr.Job.text = text;
            wr.Job.color = string.IsNullOrEmpty(work.Problem) ? GameStyle.Text : GameStyle.Danger;
        }

        private void UpdateWorkRows()
        {
            if (_workRows.Count == 0) return;
            _workTimer -= Time.unscaledDeltaTime;
            if (_workTimer > 0f) return;
            _workTimer = 1f;
            foreach (var wr in _workRows)
            {
                if (wr.Job == null) continue;
                ZombieRoster.FillJob(wr.Entry.Data, wr.Entry.Info.Work, wr.Entry.Info.Gear);
                ApplyWork(wr);
            }
        }

        // Подсказка по наведению: пишем текст в строку-подсказку панели.
        private TextMeshProUGUI _gearTip;

        private void ShowGearTip(string text)
        {
            if (_gearTip == null) return;
            _gearTip.text = text ?? string.Empty;
        }

        internal sealed class GearTipHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
        {
            internal string Tip;
            internal ZombiePanel Owner;

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (Owner != null) Owner.ShowGearTip(Tip);
            }

            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (Owner != null) Owner.ShowGearTip(null);
            }
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
            GameStyle.RefreshButtonSprite();
            UiFactory.ApplyButtonSprite(_closeButton);
            foreach (Transform child in _content) Destroy(child.gameObject);
            _workRows.Clear();
            _focusables.Clear();
            _baseColors.Clear();
            _frames.Clear();

            ZombieRoster.RefreshBodies(true);
            var entries = ZombieRoster.Load();
            int limit = ZombieRoster.Limit();
            _countLabel.text = HudFormat.Count(entries.Count, limit);
            _countLabel.color = HudFormat.OverLimit(entries.Count, limit) ? GameStyle.Danger : GameStyle.Text;
            if (entries.Count == 0)
            {
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 24, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                _focusIdx = -1;
                GameStyle.ApplyFont(_root.transform);
                return;
            }

            int number = 0;
            foreach (var e in entries)
            {
                number++;
                var row = UiFactory.Rect("Row", _content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;

                // "N. Имя · Тип" — номер порядковый в текущей сортировке панели.
                LeftLabel("Name", row, RosterLogic.RowTitle(number, e.Info.Name, ZombieText.KindName(e.Info.Kind)),
                    30, TitleX, MainLineY, TitleWidth, 44);

                // Второй строкой под именем: у работающих — станция и занятие,
                // у остальных — состояние ("в хоре" / "лежит на полу").
                if (e.Info.Work != null) MakeWorkLine(row, e);
                else
                {
                    var statusText = ZombieText.StatusName(e.Info.State);
                    if (!string.IsNullOrEmpty(statusText))
                        LeftLabel("Status", row, statusText, 22, TitleX, SecondLineY, TitleWidth, 28, GameStyle.Accent);
                }

                MakeSkull(row, WhiteX, WhiteWidth, "skull", e.Info.WhiteSkulls.ToString());
                MakeSkull(row, RedX, RedWidth, "rskull", RosterLogic.RedSkulls(e.Info));

                // Снаряжение: ошейник/инструмент/броня (+ переносимое носильщика второй строкой).
                MakeGearStrip(row, e.Info);

                // Очки технологий зомби: красные/зелёные/синие кристаллы.
                var techAsset = TechAsset();
                var tech = LeftLabel("Tech", row, "", 26, TechX, MainLineY, TechWidth, 44);
                if (techAsset != null) tech.spriteAsset = techAsset;
                tech.text = RosterLogic.Tech(e.Info, techAsset != null);

                var entry = e;
                float right = ButtonRight;

                var openBtn = MakeRowButton(row, "Open", ref right);
                openBtn.onClick.AddListener(() => { SuspendFor(ReturnFrom.ZombieWindow); ZombieRoster.OpenWindow(entry); });

                var camBtn = MakeRowButton(row, "Camera", ref right);
                camBtn.onClick.AddListener(() => { SuspendFor(ReturnFrom.Camera); ZombieRoster.FocusCamera(entry); });

                if (e.Info.CanRecall)
                {
                    var rec = MakeRowButton(row, "Recall", ref right);
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                }
            }

            // Шрифт как в меню игры — на все надписи панели (заголовок, строки, кнопки).
            GameStyle.ApplyFont(_root.transform);

            // Сохраняем/восстанавливаем фокус, чтобы подсветка не пропадала.
            if (_focusables.Count > 0) FocusButton(Mathf.Clamp(_focusIdx, 0, _focusables.Count - 1));
        }
    }
}
