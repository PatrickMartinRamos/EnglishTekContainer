using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Tek.Core
{
    public class InteractiveController : MonoBehaviour
    {
        [SerializeField] private string serverRoot = "http://localhost:8080/Interactive/";
        [SerializeField] private string grade = "grade1";
        [SerializeField] private string catalogFileName = "catalog.json";
        [SerializeField] private string defaultCategory = string.Empty;
        [SerializeField] private string defaultUnit = string.Empty;
        // Optional prefix for auto-generated bundle file names: {bundlePrefix}.{grade}.{id}
        // Leave empty to use {grade}.{id} format.
        [SerializeField] private string bundlePrefix = string.Empty;
        [SerializeField] private bool refreshCatalogOnStart = true;
        [SerializeField] private ContainerReturnOverlay overlayPrefab = null;
        [SerializeField] private OverlayButtonCorner overlayButtonCorner = OverlayButtonCorner.TopLeft;
        [SerializeField] private Vector2 overlayButtonPadding = new Vector2(10f, 10f);

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

        /// <summary>
        /// Fired when the player taps an interactive that is not cached and there is no internet.
        /// Parameters: message string, the catalog entry (may be null if not found).
        /// </summary>
        public event Action<string, InteractiveCatalogEntry> GameLoadOfflineBlocked;

        public void RequestGameLoad(string gameId)
        {
            bool offline = Application.internetReachability == NetworkReachability.NotReachable;
            if (offline && !IsInteractiveCached(gameId))
            {
                InteractiveCatalogEntry entry = FindCatalogEntry(gameId);
                string title = entry != null && !string.IsNullOrWhiteSpace(entry.title) ? entry.title : gameId;
                string msg = "Connect to the internet to download \"" + title + "\".";
                Debug.LogWarning("[InteractiveController] " + msg);
                GameLoadOfflineBlocked?.Invoke(msg, entry);
                return;
            }

            InteractiveCatalogEntry matchedEntry = FindCatalogEntry(gameId);
            StartCoroutine(DownloadAndStartRoutine(BuildDownloadTarget(gameId, matchedEntry)));
        }

        /// <summary>
        /// Returns true when both bundle files for the given game ID are already on disk.
        /// Use this to show a "Downloaded" badge on catalog buttons.
        /// </summary>
        public bool IsInteractiveCached(string gameId)
        {
            DownloadTarget target = BuildDownloadTarget(gameId, FindCatalogEntry(gameId));
            string dir = GetCacheDirectory(target.cacheKey);
            return File.Exists(Path.Combine(dir, target.bundleFileNameBase + ".assets"))
                && File.Exists(Path.Combine(dir, target.bundleFileNameBase + ".scenes"));
        }

        /// <summary>
        /// Returns true if the catalog JSON has been saved to disk at least once.
        /// Used by CatalogStatusOverlay to detect first-launch vs returning-launch.
        /// </summary>
        public bool IsCatalogCached()
        {
            return File.Exists(GetCatalogCachePath());
        }

        private string GetCatalogCachePath()
        {
            string safeGrade = BundleUrlHelper.NormalizeCacheKey(grade);
            return Path.Combine(Application.persistentDataPath, "CatalogCache", safeGrade + "_" + catalogFileName);
        }

        private void SaveCatalogCache(string json)
        {
            try
            {
                string path = GetCatalogCachePath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, json);
                Debug.Log("[Catalog] Saved catalog cache to: " + path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Catalog] Failed to save catalog cache: " + ex.Message);
            }
        }

        private string TryLoadCatalogCache()
        {
            try
            {
                string path = GetCatalogCachePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        Debug.Log("[Catalog] Loaded catalog from local cache: " + path);
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

        private IEnumerator LoadCatalogRoutine()
        {
            string catalogUrl = BuildCatalogUrl();
            UnityWebRequest request = UnityWebRequest.Get(catalogUrl);
            yield return request.SendWebRequest();

            catalogLoadRoutine = null;

            string json = null;

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogWarning("[Catalog] Network unavailable (" + request.error + "). Trying local cache...");
                json = TryLoadCatalogCache();
                if (json == null)
                {
                    string message = "Catalog download failed and no local cache found. URL: " + catalogUrl;
                    Debug.LogWarning(message);
                    CatalogLoadFailed?.Invoke(message);
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
                    CatalogLoadFailed?.Invoke(message);
                    yield break;
                }
                SaveCatalogCache(json);
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
                manifest.gameId = ParseGameId(gameId);
                Debug.Log("[Download] Recovered manifest using scene-bundle fallback. Scene: " + fallbackSceneName);
            }
            else
            {
                Debug.LogError("[Download] No scenes found in scene bundle! Bundle may be built for wrong platform.");
            }

            if (manifest != null && !string.IsNullOrEmpty(manifest.firstSceneName))
            {
                GameSession.CurrentManifest = manifest;
                GameSession.ContainerSceneName = SceneManager.GetActiveScene().name;
                ContainerReturnOverlay.EnsureExists(overlayPrefab, overlayButtonCorner, overlayButtonPadding);
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
            return BuildRootUrl() + BundleUrlHelper.EncodePathSegments(BundleUrlHelper.NormalizePathPart(grade)) + "/" + catalogFileName;
        }

        private string BuildFolderUrl(string folderName)
        {
            return BuildRootUrl() + BundleUrlHelper.EncodePathSegments(BundleUrlHelper.NormalizePathPart(folderName)) + "/";
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
                string defaultEntryFolder = BundleUrlHelper.BuildDefaultFolderPath(entryGrade, entry.category, entry.unit, entry.id);
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
            string lookupId = BundleUrlHelper.NormalizeLookupId(gameId);
            foreach (InteractiveCatalogEntry entry in availableInteractives)
            {
                if (entry == null)
                {
                    continue;
                }

                if (BundleUrlHelper.NormalizeLookupId(entry.id) == lookupId)
                {
                    return entry;
                }
            }

            return null;
        }

        private DownloadTarget BuildDownloadTarget(string gameId, InteractiveCatalogEntry entry)
        {
            string effectiveGrade    = grade;
            string effectiveCategory = defaultCategory;
            string effectiveUnit     = defaultUnit;
            string effectiveFolder     = BundleUrlHelper.BuildDefaultFolderPath(effectiveGrade, effectiveCategory, effectiveUnit, gameId);
            string effectiveBundleBase = BundleUrlHelper.BuildDefaultBundleBaseName(bundlePrefix, grade, gameId);

            if (entry != null)
            {
                if (!string.IsNullOrWhiteSpace(entry.grade))    effectiveGrade    = entry.grade;
                if (!string.IsNullOrWhiteSpace(entry.category)) effectiveCategory = entry.category;
                if (!string.IsNullOrWhiteSpace(entry.unit))     effectiveUnit     = entry.unit;

                effectiveFolder = !string.IsNullOrWhiteSpace(entry.folder)
                    ? entry.folder
                    : BundleUrlHelper.BuildDefaultFolderPath(effectiveGrade, effectiveCategory, effectiveUnit, gameId);

                effectiveBundleBase = !string.IsNullOrWhiteSpace(entry.bundleBaseName)
                    ? entry.bundleBaseName
                    : BundleUrlHelper.BuildDefaultBundleBaseName(bundlePrefix, effectiveGrade, gameId);
            }

            return new DownloadTarget
            {
                requestedId        = gameId,
                folderName         = effectiveFolder,
                bundleFileNameBase  = effectiveBundleBase,
                cacheKey           = BundleUrlHelper.BuildCacheKey(gameId, effectiveBundleBase, entry != null ? entry.bundleVersion : null)
            };
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

        private static int ParseGameId(string gameId)
        {
            if (string.IsNullOrEmpty(gameId)) return 0;
            string digits = gameId.ToUpper().StartsWith("ID") ? gameId.Substring(2) : gameId;
            return int.TryParse(digits, out int id) ? id : 0;
        }
    }    
}

