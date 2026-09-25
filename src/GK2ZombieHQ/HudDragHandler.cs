using UnityEngine;
using UnityEngine.EventSystems;

namespace GK2ZombieHQ
{
    // Перетаскивание HUD мышью (позиция следует за курсором) и клик по нему
    // открывает панель «Зомби-штаб» (как F8). Позиция сохраняется в конфиг.
    internal sealed class HudDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        internal RectTransform CanvasRect;
        internal RectTransform Target;
        internal ZombiePanel Panel;

        private bool _dragged;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragged = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (CanvasRect == null || Target == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasRect, eventData.position, null, out local)) return;

            Vector2 size = CanvasRect.rect.size;
            float w = Target.rect.width;
            float h = Target.rect.height;

            // Canvas pivot (0.5,0.5): курсор ставим в левый-верхний угол плашки (её pivot = (0,1)).
            float x = Mathf.Clamp(local.x + size.x * 0.5f, 0f, Mathf.Max(0f, size.x - w));
            float y = Mathf.Clamp(local.y - size.y * 0.5f, -Mathf.Max(0f, size.y - h), 0f);
            Target.anchoredPosition = new Vector2(x, y);

            if (eventData.delta.sqrMagnitude > 0.01f) _dragged = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Save();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragged) { _dragged = false; return; }
            if (Panel != null) Panel.Toggle();
        }

        private void Save()
        {
            if (Target == null || Plugin.Mod == null) return;
            try
            {
                Plugin.Mod.HudOffsetX.Value = Mathf.RoundToInt(Target.anchoredPosition.x);
                Plugin.Mod.HudOffsetY.Value = Mathf.RoundToInt(-Target.anchoredPosition.y);
            }
            catch { }
        }
    }
}
