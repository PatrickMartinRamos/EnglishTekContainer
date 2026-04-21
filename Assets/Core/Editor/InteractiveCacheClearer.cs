using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tek.Core.Editor
{
    public static class InteractiveCacheClearer
    {
        private static string InteractiveCacheRoot =>
            Path.Combine(Application.persistentDataPath, "InteractiveCache");

        private static string CatalogCacheRoot =>
            Path.Combine(Application.persistentDataPath, "CatalogCache");

        private static string ThumbnailCacheRoot =>
            Path.Combine(Application.persistentDataPath, "ThumbnailCache");

        [MenuItem("TekContainer/Clear Interactive Cache/All")]
        private static void ClearAll()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Clear All Cache",
                "This will delete all cached bundles, catalogs, and images at:\n" +
                Application.persistentDataPath + "\n\nProceed?",
                "Clear", "Cancel");

            if (!confirm) return;

            int cleared = 0;
            cleared += DeleteFolder(InteractiveCacheRoot);
            cleared += DeleteFolder(CatalogCacheRoot);
            cleared += DeleteFolder(ThumbnailCacheRoot);

            string msg = cleared > 0
                ? $"Cleared {cleared} cache folder(s)."
                : "No cache folders found.";

            Debug.Log("[TekContainer] " + msg);
            EditorUtility.DisplayDialog("Clear Cache", msg, "OK");
        }

        [MenuItem("TekContainer/Clear Interactive Cache/Show Cache Folder")]
        private static void ShowCacheFolder()
        {
            // Reveal persistentDataPath — all cache folders live here
            string root = Application.persistentDataPath;
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            EditorUtility.RevealInFinder(root);
        }

        private static int DeleteFolder(string path)
        {
            if (!Directory.Exists(path)) return 0;
            Directory.Delete(path, true);
            Debug.Log("[TekContainer] Deleted: " + path);
            return 1;
        }
    }
}
