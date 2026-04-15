using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EnglishTek.Core
{
    [Serializable]
    public class InteractiveCatalogEntry
    {
        public string id;
        public string title;
        public string image;
        public string category;
        public string unit;
        public string folder;
        public string grade;
        public string bundleBaseName;
        public bool enabled = true;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }

                return id;
            }
        }
    }

    [Serializable]
    public class InteractiveCatalogDocument
    {
        public List<InteractiveCatalogEntry> interactives = new List<InteractiveCatalogEntry>();
    }

    public class InteractiveController : MonoBehaviour
    {
        [SerializeField] private string serverRoot = "http://localhost:8080/Interactive/";
        [SerializeField] private string grade = "grade1";
        [SerializeField] private string catalogFileName = "catalog.json";
        [SerializeField] private string defaultCategory = string.Empty;
        [SerializeField] private string defaultUnit = string.Empty;
        [SerializeField] private bool refreshCatalogOnStart = true;

        private readonly List<InteractiveCatalogEntry> availableInteractives = new List<InteractiveCatalogEntry>();
        private Coroutine catalogLoadRoutine;

        public IReadOnlyList<InteractiveCatalogEntry> AvailableInteractives => availableInteractives;
        public event Action<IReadOnlyList<InteractiveCatalogEntry>> CatalogUpdated;
        public event Action<string> CatalogLoadFailed;

        private struct DownloadTarget
        {
            public string requestedId;
            public string folderName;
            public string selectedGrade;
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

        IEnumerator DownloadAndStartRoutine(DownloadTarget target)
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

            // Load Manifest
            // IMPORTANT: Ensure the string "Manifest_" + gameId matches asset name in Unity

            InteractiveManifest manifest = null;
            string[] assetNames = GameSession.CurrentAssetBundle.GetAllAssetNames();

            // Skip direct manifest asset deserialization from external bundles because a
            // script/assembly mismatch can emit missing-script warnings in runtime logs.
            // Use scene-bundle fallback to determine startup scene and continue gameplay.

            if (manifest == null)
            {
                string fallbackSceneName = TryGetFirstSceneNameFromSceneBundle(GameSession.CurrentSceneBundle);
                if (!string.IsNullOrEmpty(fallbackSceneName))
                {
                    manifest = ScriptableObject.CreateInstance<InteractiveManifest>();
                    manifest.firstSceneName = fallbackSceneName;
                    Debug.Log("Recovered manifest using scene-bundle fallback. Scene: " + fallbackSceneName);
                }
            }

            if (manifest != null)
            {
                EnrichManifestFromBundleIfNeeded(manifest, gameId);
            }

            if (manifest != null && !string.IsNullOrEmpty(manifest.firstSceneName))
            {
                GameSession.CurrentManifest = manifest;
                // This uses the string "Title" 
                UnityEngine.SceneManagement.SceneManager.LoadScene(manifest.firstSceneName, LoadSceneMode.Additive);
            }
            else
            {
                Debug.LogError("Could not find any InteractiveManifest asset in the bundle! Available assets: " + string.Join(", ", assetNames));
            }
        }

        private string GetCacheDirectory(string gameId)
        {
            return Path.Combine(Application.persistentDataPath, "InteractiveCache", gameId);
        }

        private string BuildCatalogUrl()
        {
            return BuildRootUrl() + catalogFileName;
        }

        private string BuildFolderUrl(string folderName)
        {
            return BuildRootUrl() + NormalizePathPart(folderName) + "/";
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
                string defaultEntryFolder = BuildDefaultFolderPath(entry.category, entry.unit, entry.id);
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
            string effectiveFolder = BuildDefaultFolderPath(effectiveCategory, effectiveUnit, gameId);
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
                    effectiveFolder = BuildDefaultFolderPath(effectiveCategory, effectiveUnit, gameId);
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
                selectedGrade = effectiveGrade,
                bundleFileNameBase = effectiveBundleBase,
                cacheKey = NormalizeCacheKey(gameId)
            };
        }

        private string BuildDefaultBundleBaseName(string selectedGrade, string gameId)
        {
            return "englishtek." + selectedGrade.ToLowerInvariant() + "." + gameId.ToLowerInvariant();
        }

        private string BuildDefaultFolderPath(string categoryName, string unitName, string gameId)
        {
            string normalizedGameId = NormalizePathPart(gameId);
            string normalizedCategory = NormalizePathPart(categoryName);
            string normalizedUnit = NormalizePathPart(unitName);

            List<string> pathParts = new List<string>();
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

            return value.Trim().Replace("/", "_").Replace("\\", "_");
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

            UnityWebRequest req = UnityWebRequest.Get(remoteUrl);
            yield return req.SendWebRequest();

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

            AssetBundleCreateRequest remoteLoadReq = AssetBundle.LoadFromMemoryAsync(downloadedBytes);
            yield return remoteLoadReq;
            loadedBundle = remoteLoadReq.assetBundle;
            if (loadedBundle == null)
            {
                Debug.LogError("Downloaded data is not a valid " + bundleLabel + " AssetBundle. URL: " + remoteUrl);
                onLoaded(null);
                yield break;
            }

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

        private bool TryBuildManifestFromObject(UnityEngine.Object source, out InteractiveManifest manifest)
        {
            manifest = null;
            if (source == null)
            {
                return false;
            }

            InteractiveManifest typedManifest = source as InteractiveManifest;
            if (typedManifest != null && !string.IsNullOrEmpty(typedManifest.firstSceneName))
            {
                manifest = typedManifest;
                return true;
            }

            System.Type sourceType = source.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo sceneNameField = sourceType.GetField("firstSceneName", flags);
            if (sceneNameField != null && sceneNameField.FieldType == typeof(string))
            {
                string recoveredSceneName = sceneNameField.GetValue(source) as string;
                if (!string.IsNullOrEmpty(recoveredSceneName))
                {
                    manifest = ScriptableObject.CreateInstance<InteractiveManifest>();
                    manifest.firstSceneName = recoveredSceneName;

                    FieldInfo bundleNameField = sourceType.GetField("bundleName", flags);
                    if (bundleNameField != null && bundleNameField.FieldType == typeof(string))
                    {
                        manifest.bundleName = bundleNameField.GetValue(source) as string;
                    }

                    return true;
                }
            }

            // Last fallback for ScriptableObject assets with matching serialized field names.
            ScriptableObject scriptableSource = source as ScriptableObject;
            if (scriptableSource != null)
            {
                try
                {
                    string json = JsonUtility.ToJson(scriptableSource);
                    if (!string.IsNullOrEmpty(json) && json != "{}")
                    {
                        manifest = ScriptableObject.CreateInstance<InteractiveManifest>();
                        JsonUtility.FromJsonOverwrite(json, manifest);
                        if (!string.IsNullOrEmpty(manifest.firstSceneName))
                        {
                            return true;
                        }
                    }
                }
                catch (System.ArgumentException)
                {
                    // Ignore non-serializable engine-backed objects and continue searching.
                }
            }

            manifest = null;
            return false;
        }

        private bool IsLikelyManifestAssetName(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            string lowerName = assetName.ToLowerInvariant();
            return lowerName.Contains("manifest") || lowerName.EndsWith(".asset");
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

