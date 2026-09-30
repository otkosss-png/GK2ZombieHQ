using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GK2ZombieHQ
{
    // Иконка станции «как в игре»: у станков нет готовых спрайтов (это 3D-модели), поэтому
    // модель один раз снимается скрытой камерой под углом игровой камеры и кэшируется по id станции.
    // Если объект станции сейчас не заспавнен (далеко) — null, и панель берёт иконку из меню игры.
    internal static class StationSnapshot
    {
        private const int Size = 160;
        private const int SnapLayer = 31;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly HashSet<string> Failed = new HashSet<string>(StringComparer.Ordinal);

        internal static Sprite Get(WgoData station)
        {
            if (station == null || string.IsNullOrEmpty(station.id)) return null;
            Sprite sprite;
            if (Cache.TryGetValue(station.id, out sprite)) return sprite;
            if (Failed.Contains(station.id)) return null;

            Wgo wgo = null;
            try { wgo = GameScene.GetWgoViewGlobal(station.UniqueId); } catch { }
            if (wgo == null) return null; // не в сцене — попробуем в следующий раз

            try { sprite = Render(wgo, station.id); }
            catch (Exception ex) { Plugin.Log.LogWarning("snapshot " + station.id + ": " + ex.Message); sprite = null; }
            if (sprite != null) Cache[station.id] = sprite;
            else Failed.Add(station.id);
            return sprite;
        }

        private static Camera GameCamera()
        {
            var main = Camera.main;
            if (main != null) return main;
            foreach (var c in Camera.allCameras)
                if (c != null && c.enabled && c.targetTexture == null) return c;
            return Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
        }

        private static Sprite Render(Wgo wgo, string id)
        {
            var main = GameCamera();
            if (main == null) return null;

            var renderers = new List<Renderer>();
            foreach (var r in wgo.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                // Только сама модель: без частиц, следов и линий (по имени типа — без лишних ссылок).
                var typeName = r.GetType().Name;
                if (typeName == "ParticleSystemRenderer" || r is TrailRenderer || r is LineRenderer) continue;
                renderers.Add(r);
            }
            if (renderers.Count == 0) return null;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) bounds.Encapsulate(renderers[i].bounds);
            float radius = Mathf.Max(0.1f, bounds.extents.magnitude);

            var camGo = new GameObject("GK2ZombieHQ_SnapshotCamera");
            RenderTexture rt = null;
            var layers = new int[renderers.Count];
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.CopyFrom(main);
                cam.enabled = false;
                cam.targetTexture = null;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.cullingMask = 1 << SnapLayer;
                cam.aspect = 1f;
                cam.transform.rotation = main.transform.rotation;
                if (cam.orthographic)
                {
                    cam.orthographicSize = radius;
                    float dist = radius * 4f + 1f;
                    cam.transform.position = bounds.center - cam.transform.forward * dist;
                    cam.nearClipPlane = 0.01f;
                    cam.farClipPlane = dist + radius * 4f;
                }
                else
                {
                    float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    cam.transform.position = bounds.center - cam.transform.forward * dist;
                    cam.nearClipPlane = Mathf.Max(0.01f, dist - radius * 1.5f);
                    cam.farClipPlane = dist + radius * 2f;
                }

                rt = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;

                for (int i = 0; i < renderers.Count; i++)
                {
                    layers[i] = renderers[i].gameObject.layer;
                    renderers[i].gameObject.layer = SnapLayer;
                }
                try { cam.Render(); }
                finally
                {
                    for (int i = 0; i < renderers.Count; i++)
                        if (renderers[i] != null) renderers[i].gameObject.layer = layers[i];
                }

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;

                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                Dump(tex, id);
                var crop = OpaqueRect(tex);
                if (crop.width < 2 || crop.height < 2) return null; // пустой снимок
                Plugin.Log.LogInfo("snapshot " + id + ": renderers=" + renderers.Count + " ortho=" + cam.orthographic + " crop=" + crop);
                return Sprite.Create(tex, crop, new Vector2(0.5f, 0.5f), 100f);
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(camGo);
            }
        }

        // Прямоугольник непрозрачных пикселей (обрезаем пустые поля вокруг модели).
        private static Rect OpaqueRect(Texture2D tex)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 12)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
            if (maxX < 0) return new Rect(0, 0, 0, 0);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Пробная версия: снимки кладём в %TEMP%\gk2zhq_snap, чтобы посмотреть результат.
        private static void Dump(Texture2D tex, string id)
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "gk2zhq_snap");
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, id + ".png"), tex.EncodeToPNG());
            }
            catch { }
        }
    }
}
