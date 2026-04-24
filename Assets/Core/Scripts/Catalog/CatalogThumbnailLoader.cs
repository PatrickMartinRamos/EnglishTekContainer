using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Tek.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    // CatalogThumbnailLoader
    //
    // Three responsibilities, clearly separated:
    //
    //   CACHE   – static helpers that read / write PNG files to local storage.
    //             Called by anyone (CarouselHomeBackground, CatalogStatusOverlay, …).
    //
    //   DOWNLOAD – parallel coroutines that pull missing images from the server
    //              and save them to the cache. This is the ONLY place that talks
    //              to the network for images. Called once at startup via
    //              DownloadAllAndNotify().
    //
    //   DISPLAY  – synchronous helpers that read from the cache and assign the
    //              texture / sprite to a UI target. Called after download is done.
    // ─────────────────────────────────────────────────────────────────────────
    [DisallowMultipleComponent]
    internal class CatalogThumbnailLoader : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const int DownloadTimeoutSeconds = 20;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<Texture2D> loadedTextures = new List<Texture2D>();
        private Coroutine downloadAllCoroutine;

        // ═════════════════════════════════════════════════════════════════════
        // CACHE — static, synchronous
        // ═════════════════════════════════════════════════════════════════════

        private static string CacheDir =>
            Path.Combine(Application.persistentDataPath, "ThumbnailCache");

        /// <summary>Returns the local disk path for a cached image URL.</summary>
        internal static string GetImageCachePath(string url)
        {
            var sb = new System.Text.StringBuilder(url.Length);
            foreach (char c in url)
                sb.Append(char.IsLetterOrDigit(c) || c == '.' ? c : '_');
            return Path.Combine(CacheDir, sb.ToString());
        }

        /// <summary>
        /// Reads a cached image from disk. Returns null if the file is absent or corrupt.
        /// </summary>
        internal static Texture2D ReadFromCache(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            string path = GetImageCachePath(url);
            if (!File.Exists(path)) return null;

            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch { return null; }

            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes)) return tex;

            Destroy(tex);
            return null;
        }

        private static void SaveToCache(string url, Texture2D tex)
        {
            try
            {
                string path = GetImageCachePath(url);
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ImageCache] Save failed: " + ex.Message);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // DOWNLOAD — coroutines, talks to the server
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Downloads all URLs in parallel to the local cache.
        /// Fires onComplete once every download has finished (success, failure, or already cached).
        /// This is the only method that contacts the server for images.
        /// </summary>
        internal void DownloadAllAndNotify(List<string> urls, Action onComplete)
        {
            if (urls == null || urls.Count == 0) { onComplete?.Invoke(); return; }
            if (downloadAllCoroutine != null) StopCoroutine(downloadAllCoroutine);
            downloadAllCoroutine = StartCoroutine(DownloadAllRoutine(urls, onComplete));
        }

        private IEnumerator DownloadAllRoutine(List<string> urls, Action onComplete)
        {
            int total = 0;
            for (int i = 0; i < urls.Count; i++)
                if (!string.IsNullOrWhiteSpace(urls[i])) total++;

            if (total == 0) { onComplete?.Invoke(); yield break; }

            Debug.Log($"[ImageDownload] Starting — {total} images to process.");

            int completed = 0;
            for (int i = 0; i < urls.Count; i++)
            {
                string url = urls[i];
                if (string.IsNullOrWhiteSpace(url)) continue;
                StartCoroutine(DownloadOneRoutine(url, () => completed++));
            }

            // Wait for every DownloadOneRoutine to call onDone.
            // Each routine always calls onDone — on success, failure, or per-request timeout.
            while (completed < total)
                yield return null;

            Debug.Log($"[ImageDownload] All {total} images done — firing ImagesReady.");
            onComplete?.Invoke();
        }

        private IEnumerator DownloadOneRoutine(string url, Action onDone)
        {
            // Already on disk — nothing to do.
            if (File.Exists(GetImageCachePath(url)))
            {
                Debug.Log($"[ImageDownload] Cached: {url}");
                onDone?.Invoke();
                yield break;
            }

            //Debug.Log($"[ImageDownload] Downloading: {url}");

            UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
            req.timeout = DownloadTimeoutSeconds;
            req.SendWebRequest();

            // Poll manually — req.timeout is unreliable on some Android versions.
            float deadline = Time.realtimeSinceStartup + DownloadTimeoutSeconds + 5f;
            while (!req.isDone)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    req.Abort();
                    Debug.LogWarning($"[ImageDownload] Timed out: {url}");
                    break;
                }
                yield return null;
            }

            if (req.isNetworkError || req.isHttpError)
            {
                Debug.LogWarning($"[ImageDownload] Failed ({req.error}): {url}");
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                if (tex != null)
                {
                    SaveToCache(url, tex);
                    Debug.Log($"[ImageDownload] Saved: {url}");
                }
                else
                {
                    Debug.LogWarning($"[ImageDownload] Null texture: {url}");
                }
            }

            onDone?.Invoke();
        }

        // ═════════════════════════════════════════════════════════════════════
        // DISPLAY — synchronous, cache-only
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Reads the cached image for this URL and assigns it to the RawImage. No-op if not cached.</summary>
        internal void LoadInto(string url, RawImage target)
        {
            if (string.IsNullOrWhiteSpace(url) || target == null) return;
            Texture2D tex = ReadFromCache(url);
            if (tex == null) return;
            loadedTextures.Add(tex);
            target.texture = tex;
            target.color = Color.white;
        }

        /// <summary>Reads the cached image for this URL and assigns it to the Image. No-op if not cached.</summary>
        internal void LoadInto(string url, Image target)
        {
            if (string.IsNullOrWhiteSpace(url) || target == null) return;
            Texture2D tex = ReadFromCache(url);
            if (tex == null) return;
            loadedTextures.Add(tex);
            target.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            target.color = Color.white;
        }

        /// <summary>Destroys all textures created by LoadInto. Call when clearing entry buttons.</summary>
        internal void ClearTextures()
        {
            for (int i = 0; i < loadedTextures.Count; i++)
                if (loadedTextures[i] != null) Destroy(loadedTextures[i]);
            loadedTextures.Clear();
        }
    }
}
