using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;

namespace EnglishTek.Core
{
    public class InteractiveController : MonoBehaviour
    {
        [SerializeField] private string serverRoot = "http://localhost:8080/Interactive/";
        [SerializeField] private string grade = "grade1";
        [SerializeField] private string catalogFileName = "catalog.json";
        [SerializeField] private string defaultCategory = string.Empty;
        [SerializeField] private string defaultUnit = string.Empty;
        // Prefix used when auto-generating bundle file names: {bundlePrefix}.{grade}.{id}
        // e.g. "englishtek" → englishtek.grade1.id106  |  "sciencetek" → sciencetek.grade1.id106
        [SerializeField] private string bundlePrefix = "englishtek";
        [SerializeField] private bool refreshCatalogOnStart = true;
        [SerializeField] private ContainerReturnOverlay overlayPrefab = null;
        [SerializeField] private OverlayButtonCorner overlayButtonCorner = OverlayButtonCorner.TopLeft;
        [SerializeField] private Vector2 overlayButtonPadding = new Vector2(10f, 10f);
        [SerializeField] private TextMeshProUGUI debugText;

        private readonly List<InteractiveCatalogEntry> availableInteractives = new List<InteractiveCatalogEntry>();
        private Coroutine catalogLoadRoutine;

        public IReadOnlyList<InteractiveCatalogEntry> AvailableInteractives => availableInteractives;
        public event Action<IReadOnlyList<InteractiveCatalogEntry>> CatalogUpdated;
        public event Action<string> CatalogLoadFailed;

        private struct DownloadTarget
        {
            public string requestedId;
            public string folderName;
            public string bundleFileNameBase;
            public string cacheKey;
        }

        private void Start()
        {
            if (refreshCatalogOnStart)
            {
                RefreshCatalog();
            }
        }

        public void RefreshCatalog()
        {
            if (catalogLoadRoutine != null)
            {
                StopCoroutine(catalogLoadRoutine);
            }

            catalogLoadRoutine = StartCoroutine(LoadCatalogRoutine());
        }

        public void RequestGameLoad(string gameId)
        {
            InteractiveCatalogEntry matchedEntry = FindCatalogEntry(gameId);
            StartCoroutine(DownloadAndStartRoutine(BuildDownloadTarget(gameId, matchedEntry)));
        }

        private IEnumerator LoadCatalogRoutine()
        {
            string catalogUrl = BuildCatalogUrl();
            UnityWebRequest request = UnityWebRequest.Get(catalogUrl);
            yield return request.SendWebRequest();

            catalogLoadRoutine = null;

            if (request.isNetworkError || request.isHttpError)
            {
                string message = "Catalog download failed: " + request.error + " | URL: " + catalogUrl;
                Debug.LogWarning(message);
                CatalogLoadFailed?.Invoke(message);
                yield break;
            }

            string json = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json))
            {
                string message = "Catalog download returned empty JSON. URL: " + catalogUrl;
                Debug.LogWarning(message);
                CatalogLoadFailed?.Invoke(message);
                yield break;
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
                CatalogLoadFailed?.Invoke(message);
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

            availableInteractives.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            CatalogUpdated?.Invoke(availableInteractives);
        }

        private IEnumerator DownloadAndStartRoutine(DownloadTarget target)
        {
            string gameId = target.requestedId;
            string folderPath = BuildFolderUrl(target.folderName);
            string fileNameBase = target.bundleFileNameBase;
            string assetBundleUrl = folderPath + fileNameBase + ".assets";
            string sceneBundleUrl = folderPath + fileNameBase + ".scenes";

            string cacheDirectory = GetCacheDirectory(target.cacheKey);
            string assetCachePath = Path.Combine(cacheDirectory, fileNameBase + ".assets");
            string sceneCachePath = Path.Combine(cacheDirectory, fileNameBase + ".scenes");

            AssetBundle loadedAssetBundle = null;
            AssetBundle loadedSceneBundle = null;

            Debug.Log("[Download] Starting game load for: " + gameId);
            Debug.Log("[Download] Assets URL: " + assetBundleUrl);
            Debug.Log("[Download] Scenes URL: " + sceneBundleUrl);
            Debug.Log("[Download] Asset cache path: " + assetCachePath);
            Debug.Log("[Download] Scene cache path: " + sceneCachePath);

            yield return StartCoroutine(LoadBundleWithLocalCacheRoutine(assetBundleUrl, assetCachePath, "assets", bundle => loadedAssetBundle = bundle));
            if (loadedAssetBundle == null)
            {
                Debug.LogError("Asset Error: Unable to load bundle from local cache or server: " + assetBundleUrl);
                yield break;
            }

            yield return StartCoroutine(LoadBundleWithLocalCacheRoutine(sceneBundleUrl, sceneCachePath, "scenes", bundle => loadedSceneBundle = bundle));
            if (loadedSceneBundle == null)
            {
                Debug.LogError("Scene Error: Unable to load bundle from local cache or server: " + sceneBundleUrl);
                loadedAssetBundle.Unload(true);
                yield break;
            }

            // Store in Session
            GameSession.CurrentAssetBundle = loadedAssetBundle;
            GameSession.CurrentSceneBundle = loadedSceneBundle;

            // Derive first scene from the scene bundle.
            // Direct manifest deserialization from external bundles is skipped to avoid
            // missing-script warnings caused by script/assembly mismatches at runtime.
            string[] assetNames = GameSession.CurrentAssetBundle.GetAllAssetNames();
            string fallbackSceneName = TryGetFirstSceneNameFromSceneBundle(GameSession.CurrentSceneBundle);
            InteractiveManifest manifest = null;

            if (!string.IsNullOrEmpty(fallbackSceneName))
            {
                manifest = ScriptableObject.CreateInstance<InteractiveManifest>();
                manifest.firstSceneName = fallbackSceneName;
                Debug.Log("[Download] Recovered manifest using scene-bundle fallback. Scene: " + fallbackSceneName);
            }
            else
            {
                Debug.LogError("[Download] No scenes found in scene bundle! Bundle may be built for wrong platform.");
            }

            if (manifest != null)
            {
                EnrichManifestFromBundleIfNeeded(manifest, gameId);
            }

            if (manifest != null && !string.IsNullOrEmpty(manifest.firstSceneName))
            {
                GameSession.CurrentManifest = manifest;
                GameSession.ContainerSceneName = SceneManager.GetActiveScene().name;
                ContainerReturnOverlay.EnsureExists(overlayPrefab, overlayButtonCorner, overlayButtonPadding);
                // This uses the string "Title" 
                SceneManager.LoadScene(manifest.firstSceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("Could not find any InteractiveManifest asset in the bundle Available assets: " + string.Join(", ", assetNames));
            }
        }

        private string GetCacheDirectory(string gameId)
        {
            return Path.Combine(Application.persistentDataPath, "InteractiveCache", gameId);
        }

        private string BuildCatalogUrl()
        {
            return BuildRootUrl() + EncodePathSegments(NormalizePathPart(grade)) + "/" + catalogFileName;
        }

        private string BuildFolderUrl(string folderName)
        {
            return BuildRootUrl() + EncodePathSegments(NormalizePathPart(folderName)) + "/";
        }

        public string ResolveCatalogAssetUrl(InteractiveCatalogEntry entry, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            string trimmedPath = assetPath.Trim();
            if (Uri.IsWellFormedUriString(trimmedPath, UriKind.Absolute))
            {
                return trimmedPath;
            }

            if (trimmedPath.StartsWith("/"))
            {
                return BuildRootUrl().TrimEnd('/') + trimmedPath;
            }

            if (entry != null && !string.IsNullOrWhiteSpace(entry.folder))
            {
                return CombineUrl(BuildFolderUrl(entry.folder), trimmedPath);
            }

            if (entry != null)
            {
                string entryGrade = !string.IsNullOrWhiteSpace(entry.grade) ? entry.grade : grade;
                string defaultEntryFolder = BuildDefaultFolderPath(entryGrade, entry.category, entry.unit, entry.id);
                if (!string.IsNullOrWhiteSpace(defaultEntryFolder))
                {
                    return CombineUrl(BuildFolderUrl(defaultEntryFolder), trimmedPath);
                }
            }

            return CombineUrl(BuildRootUrl(), trimmedPath);
        }

        private string BuildRootUrl()
        {
            if (string.IsNullOrEmpty(serverRoot))
            {
                return string.Empty;
            }

            return serverRoot.EndsWith("/") ? serverRoot : serverRoot + "/";
        }

        private string CombineUrl(string baseUrl, string relativePath)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                return relativePath;
            }

            return baseUrl.TrimEnd('/') + "/" + relativePath.TrimStart('/');
        }

        private InteractiveCatalogEntry FindCatalogEntry(string gameId)
        {
            string lookupId = NormalizeLookupId(gameId);
            foreach (InteractiveCatalogEntry entry in availableInteractives)
            {
                if (entry == null)
                {
                    continue;
                }

                if (NormalizeLookupId(entry.id) == lookupId)
                {
                    return entry;
                }
            }

            return null;
        }

        private DownloadTarget BuildDownloadTarget(string gameId, InteractiveCatalogEntry entry)
        {
            string effectiveGrade = grade;
            string effectiveCategory = defaultCategory;
            string effectiveUnit = defaultUnit;
            string effectiveFolder = BuildDefaultFolderPath(effectiveGrade, effectiveCategory, effectiveUnit, gameId);
            string effectiveBundleBase = BuildDefaultBundleBaseName(grade, gameId);

            if (entry != null)
            {
                if (!string.IsNullOrWhiteSpace(entry.grade))
                {
                    effectiveGrade = entry.grade;
                }

                if (!string.IsNullOrWhiteSpace(entry.category))
                {
                    effectiveCategory = entry.category;
                }

                if (!string.IsNullOrWhiteSpace(entry.unit))
                {
                    effectiveUnit = entry.unit;
                }

                if (!string.IsNullOrWhiteSpace(entry.folder))
                {
                    effectiveFolder = entry.folder;
                }
                else
                {
                    effectiveFolder = BuildDefaultFolderPath(effectiveGrade, effectiveCategory, effectiveUnit, gameId);
                }

                if (!string.IsNullOrWhiteSpace(entry.bundleBaseName))
                {
                    effectiveBundleBase = entry.bundleBaseName;
                }
                else
                {
                    effectiveBundleBase = BuildDefaultBundleBaseName(effectiveGrade, gameId);
                }
            }

            return new DownloadTarget
            {
                requestedId = gameId,
                folderName = effectiveFolder,
                bundleFileNameBase = effectiveBundleBase,
                cacheKey = BuildCacheKey(gameId, effectiveBundleBase, entry)
            };
        }

        private string BuildCacheKey(string gameId, string bundleBaseName, InteractiveCatalogEntry entry)
        {
            string normalizedId = NormalizeCacheKey(gameId);
            string normalizedBase = NormalizeCacheKey(bundleBaseName);
            string version = entry != null ? entry.bundleVersion : null;
            if (string.IsNullOrWhiteSpace(version))
            {
                return normalizedId + "_" + normalizedBase;
            }

            return normalizedId + "_" + normalizedBase + "_" + NormalizeCacheKey(version);
        }

        private string BuildDefaultBundleBaseName(string selectedGrade, string gameId)
        {
            string safeGrade = selectedGrade.ToLowerInvariant().Replace(" ", string.Empty);
            string prefix = string.IsNullOrWhiteSpace(bundlePrefix) ? "englishtek" : bundlePrefix.Trim().ToLowerInvariant();
            return prefix + "." + safeGrade + "." + gameId.ToLowerInvariant();
        }

        private string BuildDefaultFolderPath(string selectedGrade, string categoryName, string unitName, string gameId)
        {
            string normalizedGrade    = NormalizePathPart(selectedGrade);
            string normalizedGameId   = NormalizePathPart(gameId);
            string normalizedCategory = NormalizePathPart(categoryName);
            string normalizedUnit     = NormalizePathPart(unitName);

            List<string> pathParts = new List<string>();
            if (!string.IsNullOrEmpty(normalizedGrade))
            {
                pathParts.Add(normalizedGrade);
            }

            if (!string.IsNullOrEmpty(normalizedCategory))
            {
                pathParts.Add(normalizedCategory);
            }

            if (!string.IsNullOrEmpty(normalizedUnit))
            {
                pathParts.Add(normalizedUnit);
            }

            if (!string.IsNullOrEmpty(normalizedGameId))
            {
                pathParts.Add(normalizedGameId);
            }

            if (pathParts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("/", pathParts.ToArray());
        }

        private string NormalizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().Replace("\\", "/").Trim('/');
        }

        private string NormalizeLookupId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim().ToUpperInvariant();
            if (!normalized.StartsWith("ID"))
            {
                normalized = "ID" + normalized;
            }

            return normalized;
        }

        private string NormalizeCacheKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Trim().Replace("/", "_").Replace("\\", "_").Replace(" ", "_");
        }

        // Encodes each slash-separated segment of a path for use in a URL.
        // e.g. "Grade 1/grammar/unit1" → "Grade%201/grammar/unit1"
        private static string EncodePathSegments(string path)
        {
            if (string.IsNullOrEmpty(path)) { return path; }
            string[] segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Uri.EscapeDataString(segments[i]);
            }
            return string.Join("/", segments);
        }

        private IEnumerator LoadBundleWithLocalCacheRoutine(string remoteUrl, string localPath, string bundleLabel, Action<AssetBundle> onLoaded)
        {
            AssetBundle loadedBundle = null;

            if (File.Exists(localPath))
            {
                byte[] localBytes = null;
                try
                {
                    localBytes = File.ReadAllBytes(localPath);
                }
                catch (Exception readEx)
                {
                    Debug.LogWarning("Failed to read local " + bundleLabel + " cache. Will re-download. Path: " + localPath + " | " + readEx.Message);
                }

                if (localBytes != null && localBytes.Length > 0)
                {
                    AssetBundleCreateRequest localLoadReq = AssetBundle.LoadFromMemoryAsync(localBytes);
                    yield return localLoadReq;
                    loadedBundle = localLoadReq.assetBundle;
                    if (loadedBundle != null)
                    {
                        Debug.Log("Loaded " + bundleLabel + " bundle from local cache: " + localPath);
                        onLoaded(loadedBundle);
                        yield break;
                    }

                    Debug.LogWarning("Local " + bundleLabel + " cache is invalid. Will re-download. Path: " + localPath);
                    TryDeleteFile(localPath);
                }
            }

            Debug.Log("[Download] Fetching " + bundleLabel + " from: " + remoteUrl);
            
            UnityWebRequest req = UnityWebRequest.Get(remoteUrl);
            req.SendWebRequest();
            while (!req.isDone)
            {
                    if (debugText != null)
                        debugText.text = "[Download] " + bundleLabel + " progress: " + Mathf.RoundToInt(req.downloadProgress * 100f) + "%";
                yield return null;
            }
            Debug.Log("[Download] " + bundleLabel + " progress: 100%");

            if (req.isNetworkError || req.isHttpError)
            {
                Debug.LogError("Download failed for " + bundleLabel + " bundle: " + req.error + " | URL: " + remoteUrl);
                onLoaded(null);
                yield break;
            }

            byte[] downloadedBytes = req.downloadHandler.data;
            if (downloadedBytes == null || downloadedBytes.Length == 0)
            {
                Debug.LogError("Downloaded " + bundleLabel + " bundle is empty. URL: " + remoteUrl);
                onLoaded(null);
                yield break;
            }

            Debug.Log("[Download] " + bundleLabel + " downloaded " + downloadedBytes.Length + " bytes. Loading as AssetBundle...");
            AssetBundleCreateRequest remoteLoadReq = AssetBundle.LoadFromMemoryAsync(downloadedBytes);
            yield return remoteLoadReq;
            loadedBundle = remoteLoadReq.assetBundle;
            if (loadedBundle == null)
            {
                Debug.LogError("[Download] Downloaded data is NOT a valid " + bundleLabel + " AssetBundle. URL: " + remoteUrl + " | This usually means the bundle was built for a different platform (e.g. WebGL/Standalone instead of Android).");
                onLoaded(null);
                yield break;
            }
            Debug.Log("[Download] " + bundleLabel + " AssetBundle loaded successfully.");

            try
            {
                string directory = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(localPath, downloadedBytes);
                Debug.Log("Saved " + bundleLabel + " bundle to local cache: " + localPath);
            }
            catch (Exception writeEx)
            {
                Debug.LogWarning("Loaded " + bundleLabel + " bundle but failed to cache locally: " + writeEx.Message);
            }

            onLoaded(loadedBundle);
        }

        private void TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception deleteEx)
            {
                Debug.LogWarning("Failed to delete invalid cache file: " + filePath + " | " + deleteEx.Message);
            }
        }

        private string TryGetFirstSceneNameFromSceneBundle(AssetBundle sceneBundle)
        {
            if (sceneBundle == null)
            {
                return null;
            }

            string[] scenePaths = sceneBundle.GetAllScenePaths();
            if (scenePaths == null || scenePaths.Length == 0)
            {
                return null;
            }

            // Bundle order is not guaranteed to match gameplay flow.
            // Prefer Title when present, then fall back to the first listed scene.
            foreach (string scenePath in scenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (string.Equals(sceneName, "Title", System.StringComparison.OrdinalIgnoreCase))
                {
                    return sceneName;
                }
            }

            return Path.GetFileNameWithoutExtension(scenePaths[0]);
        }

        private void EnrichManifestFromBundleIfNeeded(InteractiveManifest manifest, string gameId)
        {
            if (manifest == null || GameSession.CurrentAssetBundle == null)
            {
                return;
            }

            if (manifest.xmlConfigs == null)
            {
                manifest.xmlConfigs = new List<NamedXML>();
            }

            HashSet<string> existingKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (NamedXML config in manifest.xmlConfigs)
            {
                if (config != null && !string.IsNullOrEmpty(config.key))
                {
                    existingKeys.Add(config.key);
                }
            }

            string[] assetNames = GameSession.CurrentAssetBundle.GetAllAssetNames();
            foreach (string assetName in assetNames)
            {
                if (!assetName.EndsWith(".xml"))
                {
                    continue;
                }

                TextAsset xmlAsset = GameSession.CurrentAssetBundle.LoadAsset<TextAsset>(assetName);
                if (xmlAsset == null)
                {
                    continue;
                }

                string key = GetLikelyManifestXmlKey(assetName, gameId);
                if (existingKeys.Contains(key))
                {
                    continue;
                }

                manifest.xmlConfigs.Add(new NamedXML
                {
                    key = key,
                    xmlFile = xmlAsset
                });
                existingKeys.Add(key);
            }
        }

        private string GetLikelyManifestXmlKey(string assetPath, string gameId)
        {
            string lower = assetPath.ToLower();
            string normalizedId = gameId;
            if (!string.IsNullOrEmpty(normalizedId) && !normalizedId.ToUpper().StartsWith("ID"))
            {
                normalizedId = "ID" + normalizedId;
            }

            if (lower.Contains("instruction")) return "Instruction_ET1" + normalizedId;
            if (lower.Contains("feedback")) return "Feedback_ET1" + normalizedId;
            if (lower.Contains("practice")) return "ItemBankPractice_ET1" + normalizedId;
            if (lower.Contains("workout")) return "ItembankWorkout_ET1" + normalizedId;
            if (lower.Contains("quiz")) return "ItembankQuiz_ET1" + normalizedId;

            return Path.GetFileNameWithoutExtension(assetPath);
        }
    }    
}

