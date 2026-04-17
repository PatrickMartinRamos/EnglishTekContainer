using System.IO;
using UnityEditor;
using UnityEngine;

namespace EnglishTek.Core.Editor
{
    public static class InteractiveCacheClearer
    {
        private static string CacheRoot =>
            Path.Combine(Application.persistentDataPath, "InteractiveCache");

        [MenuItem("EnglishTek/Clear Interactive Cache/All")]
        private static void ClearAll()
        {
            if (!Directory.Exists(CacheRoot))
            {
                EditorUtility.DisplayDialog("Clear Cache", "Cache folder does not exist:\n" + CacheRoot, "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Clear All Interactive Cache",
                "This will delete all cached bundles at:\n" + CacheRoot + "\n\nProceed?",
                "Clear", "Cancel");

            if (!confirm) return;

            Directory.Delete(CacheRoot, true);
            Debug.Log("[EnglishTek] Interactive cache cleared: " + CacheRoot);
            EditorUtility.DisplayDialog("Clear Cache", "All interactive cache cleared.", "OK");
        }

        [MenuItem("EnglishTek/Clear Interactive Cache/ID106")]
        private static void ClearID106() => ClearById("ID106");

        [MenuItem("EnglishTek/Clear Interactive Cache/ID213")]
        private static void ClearID213() => ClearById("ID213");

        [MenuItem("EnglishTek/Clear Interactive Cache/ID232")]
        private static void ClearID232() => ClearById("ID232");

        [MenuItem("EnglishTek/Clear Interactive Cache/Show Cache Folder")]
        private static void ShowCacheFolder()
        {
            if (!Directory.Exists(CacheRoot))
                Directory.CreateDirectory(CacheRoot);

            EditorUtility.RevealInFinder(CacheRoot);
        }

        private static void ClearById(string gameId)
        {
            // Cache keys are built as {normalizedId}_{normalizedBundleBase}[_{version}]
            // so we match any subfolder starting with the normalized id.
            string normalizedId = gameId.Trim().Replace("/", "_").Replace("\\", "_").Replace(" ", "_").ToLower();

            if (!Directory.Exists(CacheRoot))
            {
                EditorUtility.DisplayDialog("Clear Cache", "Cache folder does not exist:\n" + CacheRoot, "OK");
                return;
            }

            string[] subdirs = Directory.GetDirectories(CacheRoot);
            int deleted = 0;
            foreach (string dir in subdirs)
            {
                string dirName = Path.GetFileName(dir).ToLower();
                if (dirName.StartsWith(normalizedId))
                {
                    Directory.Delete(dir, true);
                    Debug.Log("[EnglishTek] Deleted cache: " + dir);
                    deleted++;
                }
            }

            if (deleted == 0)
            {
                EditorUtility.DisplayDialog("Clear Cache", "No cached bundles found for " + gameId, "OK");
            }
            else
            {
                Debug.Log("[EnglishTek] Cleared " + deleted + " cache folder(s) for " + gameId);
                EditorUtility.DisplayDialog("Clear Cache", "Cleared cache for " + gameId + " (" + deleted + " folder(s) removed).", "OK");
            }
        }
    }
}
