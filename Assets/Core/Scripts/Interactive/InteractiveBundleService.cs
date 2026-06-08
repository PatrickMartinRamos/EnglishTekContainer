using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Tek.Core
{
    internal static class InteractiveBundleService
    {
        internal enum BundleLoadFailureKind
        {
            None,
            Network,
            Empty,
            InvalidData
        }

        internal struct DownloadTarget
        {
            public string requestedId;
            public string folderName;
            public string bundleFileNameBase;
            public string cacheKey;
        }

        internal static DownloadTarget BuildDownloadTarget(
            string gameId,
            InteractiveCatalogEntry entry,
            GradeLevel grade,
            string defaultCategory,
            string defaultUnit,
            CurrentTek currentTek,
            BundlePrefix bundlePrefix)
        {
            BundlePrefix effectivePrefix = ResolveBundlePrefix(currentTek, bundlePrefix);
            string effectiveGrade = InteractivePathResolver.GetGradePathName(grade);
            string effectiveCategory = defaultCategory;
            string effectiveUnit = defaultUnit;
            string effectiveFolder = BundleUrlHelper.BuildDefaultFolderPath(effectiveGrade, effectiveCategory, effectiveUnit, gameId);
            string effectiveBundleBase = BundleUrlHelper.BuildDefaultBundleBaseName(effectivePrefix.ToString(), InteractivePathResolver.GetGradePathName(grade), gameId);

            if (entry != null)
            {
                if (!string.IsNullOrWhiteSpace(entry.grade)) effectiveGrade = entry.grade;
                if (!string.IsNullOrWhiteSpace(entry.category)) effectiveCategory = entry.category;
                if (!string.IsNullOrWhiteSpace(entry.unit)) effectiveUnit = entry.unit;

                effectiveFolder = !string.IsNullOrWhiteSpace(entry.folder)
                    ? entry.folder
                    : BundleUrlHelper.BuildDefaultFolderPath(effectiveGrade, effectiveCategory, effectiveUnit, gameId);

                effectiveBundleBase = !string.IsNullOrWhiteSpace(entry.bundleBaseName)
                    ? entry.bundleBaseName
                    : BundleUrlHelper.BuildDefaultBundleBaseName(effectivePrefix.ToString(), effectiveGrade, gameId);
            }

            return new DownloadTarget
            {
                requestedId = gameId,
                folderName = effectiveFolder,
                bundleFileNameBase = effectiveBundleBase,
                cacheKey = BundleUrlHelper.BuildCacheKey(gameId, effectiveBundleBase, entry != null ? entry.bundleVersion : null)
            };
        }

        private static BundlePrefix ResolveBundlePrefix(CurrentTek currentTek, BundlePrefix fallbackPrefix)
        {
            if (Enum.TryParse(currentTek.ToString(), true, out BundlePrefix parsedPrefix))
            {
                return parsedPrefix;
            }

            return fallbackPrefix;
        }

        internal static string GetCacheDirectory(string cacheKey)
        {
            return Path.Combine(Application.persistentDataPath, "InteractiveCache", cacheKey);
        }

        internal static IEnumerator DownloadAndStartRoutine(
            DownloadTarget target,
            InteractiveCatalogEntry entry,
            Func<string, string> buildFolderUrl,
            Func<string, bool> isInteractiveCached,
            Action<string, InteractiveCatalogEntry> onOfflineBlocked,
            Action onGameLoadFinished,
            ContainerReturnOverlay overlayPrefab,
            OverlayButtonCorner overlayButtonCorner,
            Vector2 overlayButtonPadding)
        {
            if (GameSession.CurrentAssetBundle != null || GameSession.CurrentSceneBundle != null)
            {
                GameSession.CleanUp();
            }

            string gameId = target.requestedId;
            string folderPath = buildFolderUrl(target.folderName);
            string fileNameBase = target.bundleFileNameBase;
            string assetBundleUrl = folderPath + fileNameBase + ".assets";
            string sceneBundleUrl = folderPath + fileNameBase + ".scenes";

            string cacheDirectory = GetCacheDirectory(target.cacheKey);
            string assetCachePath = Path.Combine(cacheDirectory, fileNameBase + ".assets");
            string sceneCachePath = Path.Combine(cacheDirectory, fileNameBase + ".scenes");

            AssetBundle loadedAssetBundle = null;
            AssetBundle loadedSceneBundle = null;
            BundleLoadFailureKind assetFailureKind = BundleLoadFailureKind.None;
            BundleLoadFailureKind sceneFailureKind = BundleLoadFailureKind.None;

            yield return LoadBundleWithLocalCacheRoutine(assetBundleUrl, assetCachePath, "assets", (bundle, failureKind) =>
            {
                loadedAssetBundle = bundle;
                assetFailureKind = failureKind;
            });

            if (loadedAssetBundle == null)
            {
                HandleBundleLoadFailure(gameId, entry, assetFailureKind, "assets", assetBundleUrl, isInteractiveCached, onOfflineBlocked);
                onGameLoadFinished?.Invoke();
                Debug.LogError("Asset Error: Unable to load bundle from local cache or server: " + assetBundleUrl);
                yield break;
            }

            yield return LoadBundleWithLocalCacheRoutine(sceneBundleUrl, sceneCachePath, "scenes", (bundle, failureKind) =>
            {
                loadedSceneBundle = bundle;
                sceneFailureKind = failureKind;
            });

            if (loadedSceneBundle == null)
            {
                HandleBundleLoadFailure(gameId, entry, sceneFailureKind, "scenes", sceneBundleUrl, isInteractiveCached, onOfflineBlocked);
                onGameLoadFinished?.Invoke();
                Debug.LogError("Scene Error: Unable to load bundle from local cache or server: " + sceneBundleUrl);
                loadedAssetBundle.Unload(true);
                yield break;
            }

            GameSession.CurrentAssetBundle = loadedAssetBundle;
            GameSession.CurrentSceneBundle = loadedSceneBundle;

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
                BundleTMPShaderFixer.EnsureExists();
                AspectRatioEnforcer enforcer = UnityEngine.Object.FindObjectOfType<AspectRatioEnforcer>();
                if (enforcer != null)
                {
                    enforcer.EnableEnforcement();
                }

                SceneManager.LoadScene(manifest.firstSceneName, LoadSceneMode.Single);
            }
            else
            {
                onGameLoadFinished?.Invoke();
                Debug.LogError("Could not find any InteractiveManifest asset in the bundle Available assets: " + string.Join(", ", assetNames));
            }
        }

        private static void HandleBundleLoadFailure(
            string gameId,
            InteractiveCatalogEntry entry,
            BundleLoadFailureKind failureKind,
            string bundleLabel,
            string remoteUrl,
            Func<string, bool> isInteractiveCached,
            Action<string, InteractiveCatalogEntry> onOfflineBlocked)
        {
            if (failureKind != BundleLoadFailureKind.Network || isInteractiveCached(gameId))
            {
                return;
            }

            string title = entry != null && !string.IsNullOrWhiteSpace(entry.title) ? entry.title : gameId;
            string msg = "Connect to the internet to download \"" + title + "\".";
            Debug.LogWarning("[InteractiveController] " + msg + " Bundle: " + bundleLabel + " URL: " + remoteUrl);
            onOfflineBlocked?.Invoke(msg, entry);
        }

        private static IEnumerator LoadBundleWithLocalCacheRoutine(string remoteUrl, string localPath, string bundleLabel, Action<AssetBundle, BundleLoadFailureKind> onLoaded)
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
                        onLoaded(loadedBundle, BundleLoadFailureKind.None);
                        yield break;
                    }

                    Debug.LogWarning("Local " + bundleLabel + " cache is invalid. Will re-download. Path: " + localPath);
                    TryDeleteFile(localPath);
                }
            }

            Debug.Log("[Download] Fetching " + bundleLabel + " from: " + remoteUrl);

            byte[] downloadedBytes = null;
            using (UnityWebRequest req = UnityWebRequest.Get(remoteUrl))
            {
                yield return req.SendWebRequest();

                if (req.isNetworkError || req.isHttpError)
                {
                    Debug.LogError("Download failed for " + bundleLabel + " bundle: " + req.error + " | URL: " + remoteUrl);
                    onLoaded(null, BundleLoadFailureKind.Network);
                    yield break;
                }

                downloadedBytes = req.downloadHandler.data;
            }

            if (downloadedBytes == null || downloadedBytes.Length == 0)
            {
                Debug.LogError("Downloaded " + bundleLabel + " bundle is empty. URL: " + remoteUrl);
                onLoaded(null, BundleLoadFailureKind.Empty);
                yield break;
            }

            Debug.Log("[Download] " + bundleLabel + " downloaded " + downloadedBytes.Length + " bytes. Loading as AssetBundle...");
            AssetBundleCreateRequest remoteLoadReq = AssetBundle.LoadFromMemoryAsync(downloadedBytes);
            yield return remoteLoadReq;
            loadedBundle = remoteLoadReq.assetBundle;
            if (loadedBundle == null)
            {
                Debug.LogError("[Download] Downloaded data is NOT a valid " + bundleLabel + " AssetBundle. URL: " + remoteUrl + " | This usually means the bundle was built for a different platform (e.g. WebGL/Standalone instead of Android).");
                onLoaded(null, BundleLoadFailureKind.InvalidData);
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

            onLoaded(loadedBundle, BundleLoadFailureKind.None);
        }

        private static void TryDeleteFile(string filePath)
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

        private static string TryGetFirstSceneNameFromSceneBundle(AssetBundle sceneBundle)
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

            foreach (string scenePath in scenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (string.Equals(sceneName, "Title", StringComparison.OrdinalIgnoreCase))
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
