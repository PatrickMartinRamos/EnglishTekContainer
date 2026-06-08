using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Tek.Core
{
    internal static class InteractiveCatalogService
    {
        internal static string GetCatalogCachePath(bool useGoogleSheetCatalogs, CurrentTek currentTek, GradeLevel grade, string catalogFileName)
        {
            string tekName = InteractivePathResolver.GetTekPathName(currentTek);
            string gradeName = InteractivePathResolver.GetGradePathName(grade);
            string cacheScope = useGoogleSheetCatalogs ? tekName : (tekName + "/" + gradeName);
            string safeScope = BundleUrlHelper.NormalizeCacheKey(cacheScope);
            return Path.Combine(Application.persistentDataPath, "CatalogCache", safeScope + "_" + catalogFileName);
        }

        internal static bool IsCatalogCached(string cachePath)
        {
            return File.Exists(cachePath);
        }

        internal static void SaveCatalogCache(string cachePath, string json)
        {
            try
            {
                string dir = Path.GetDirectoryName(cachePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(cachePath, json);
                Debug.Log("[Catalog] Saved catalog cache to: " + cachePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Catalog] Failed to save catalog cache: " + ex.Message);
            }
        }

        internal static string TryLoadCatalogCache(string cachePath)
        {
            try
            {
                if (File.Exists(cachePath))
                {
                    string json = File.ReadAllText(cachePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        Debug.Log("[Catalog] Loaded catalog from local cache: " + cachePath);
                        return json;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Catalog] Failed to read catalog cache: " + ex.Message);
            }
            return null;
        }

        internal static IEnumerator LoadCatalogRoutine(
            string catalogUrl,
            string cachePath,
            List<InteractiveCatalogEntry> availableInteractives,
            Action<IReadOnlyList<InteractiveCatalogEntry>> onCatalogUpdated,
            Action<string> onCatalogLoadFailed,
            Action onRoutineCompleted)
        {
            string json = null;

            using (UnityWebRequest request = UnityWebRequest.Get(catalogUrl))
            {
                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogWarning("[Catalog] Network unavailable (" + request.error + "). Trying local cache...");
                    json = TryLoadCatalogCache(cachePath);
                    if (json == null)
                    {
                        string message = "Catalog download failed and no local cache found. URL: " + catalogUrl;
                        Debug.LogWarning(message);
                        onCatalogLoadFailed?.Invoke(message);
                        onRoutineCompleted?.Invoke();
                        yield break;
                    }
                }
                else
                {
                    json = request.downloadHandler.text;
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        string message = "Catalog download returned empty JSON. URL: " + catalogUrl;
                        Debug.LogWarning(message);
                        onCatalogLoadFailed?.Invoke(message);
                        onRoutineCompleted?.Invoke();
                        yield break;
                    }

                    SaveCatalogCache(cachePath, json);
                }
            }

            InteractiveCatalogDocument catalog = null;
            try
            {
                catalog = JsonUtility.FromJson<InteractiveCatalogDocument>(json);
            }
            catch (ArgumentException ex)
            {
                string message = "Catalog JSON could not be parsed. " + ex.Message;
                Debug.LogWarning(message);
                onCatalogLoadFailed?.Invoke(message);
                onRoutineCompleted?.Invoke();
                yield break;
            }

            availableInteractives.Clear();
            if (catalog != null && catalog.interactives != null)
            {
                foreach (InteractiveCatalogEntry entry in catalog.interactives)
                {
                    if (entry == null || !entry.enabled || string.IsNullOrWhiteSpace(entry.id))
                    {
                        continue;
                    }

                    availableInteractives.Add(entry);
                }
            }

            availableInteractives.Sort((left, right) =>
                string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));

            onCatalogUpdated?.Invoke(availableInteractives);
            onRoutineCompleted?.Invoke();
        }
    }
}
