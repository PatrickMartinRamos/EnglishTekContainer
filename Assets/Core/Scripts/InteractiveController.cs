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
    public class InteractiveController : MonoBehaviour
    {
        [SerializeField] private string serverRoot = "http://localhost:8080/Interactive/";
        [SerializeField] private string grade = "grade1";

        public void RequestGameLoad(string gameId)
        {
            StartCoroutine(DownloadAndStartRoutine(gameId));
        }

        IEnumerator DownloadAndStartRoutine(string gameId)
        {
            string folderPath = serverRoot + gameId + "/"; // Assuming server organizes by game ID folders
            string fileNameBase = "englishtek." + grade.ToLowerInvariant() + "." + gameId.ToLowerInvariant();
            string assetBundleUrl = folderPath + fileNameBase + ".assets";
            string sceneBundleUrl = folderPath + fileNameBase + ".scenes";

            string cacheDirectory = GetCacheDirectory(gameId);
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

            foreach (string name in assetNames)
            {
                manifest = GameSession.CurrentAssetBundle.LoadAsset<InteractiveManifest>(name);
                if (manifest != null) 
                {
                    Debug.Log("Successfully loaded manifest from: " + name);
                    break; 
                }
            }

            if (manifest == null)
            {
                foreach (string name in assetNames)
                {
                    UnityEngine.Object rawAsset = GameSession.CurrentAssetBundle.LoadAsset(name);
                    if (TryBuildManifestFromObject(rawAsset, out manifest))
                    {
                        Debug.LogWarning("Recovered manifest from asset: " + name + " | Runtime Type: " + rawAsset.GetType().FullName);
                        break;
                    }
                }
            }

            if (manifest == null)
            {
                string fallbackSceneName = TryGetFirstSceneNameFromSceneBundle(GameSession.CurrentSceneBundle);
                if (!string.IsNullOrEmpty(fallbackSceneName))
                {
                    manifest = ScriptableObject.CreateInstance<InteractiveManifest>();
                    manifest.firstSceneName = fallbackSceneName;
                    Debug.LogWarning("Recovered manifest using scene-bundle fallback. Scene: " + fallbackSceneName);
                }
            }

            if (manifest != null)
            {
                EnrichManifestFromBundleIfNeeded(manifest, gameId);
            }

            if (manifest != null && !string.IsNullOrEmpty(manifest.firstSceneName))
            {
                GameSession.CurrentManifest = manifest;
                // This uses the string "Title" from your screenshot
                UnityEngine.SceneManagement.SceneManager.LoadScene(manifest.firstSceneName);
            }
            else
            {
                Debug.LogError("Could not find any InteractiveManifest asset in the bundle! Available assets: " + string.Join(", ", assetNames));
                UnityEngine.Object[] allAssets = GameSession.CurrentAssetBundle.LoadAllAssets();
                foreach (var obj in allAssets)
                {
                    if (TryBuildManifestFromObject(obj, out manifest))
                    {
                        string runtimeType = obj.GetType().FullName;
                        string runtimeAssembly = obj.GetType().Assembly.GetName().Name;
                        Debug.LogWarning("Recovered manifest from LoadAllAssets: " + obj.name + " | Type: " + runtimeType + " | Assembly: " + runtimeAssembly);
                        EnrichManifestFromBundleIfNeeded(manifest, gameId);
                        GameSession.CurrentManifest = manifest;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(manifest.firstSceneName);
                        break;
                    }
                }
            }
        }

        private string GetCacheDirectory(string gameId)
        {
            return Path.Combine(Application.persistentDataPath, "InteractiveCache", gameId);
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

