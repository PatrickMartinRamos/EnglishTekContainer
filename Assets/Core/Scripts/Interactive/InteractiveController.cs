using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Tek.Core
{
    public class InteractiveController : MonoBehaviour
    {
        private const string CacheVersionPlayerPrefsKey = "Tek.Core.CacheVersion";
        private const string catalogFileName = "catalog.json";

        [SerializeField] private string serverRoot = "";
        [SerializeField] private bool useGoogleSheetCatalogs = true;
        [SerializeField] private string webAppUrl = "https://script.google.com/macros/s/AKfycbyTwL-5-F72fVD9lX63CqOESbTFvWKPn1Iu1AcFIC_7bKWH2rLACFmFLOHBb4edr6ln/exec";
        [SerializeField] private GradeLevel grade = GradeLevel.Grade1;
        [SerializeField] private CurrentTek currentTek = CurrentTek.englishtek;
        private BundlePrefix bundlePrefix = BundlePrefix.englishtek;
        [SerializeField] private bool refreshCatalogOnStart = true;
        [SerializeField] private ContainerReturnOverlay overlayPrefab = null;
        [SerializeField] private OverlayButtonCorner overlayButtonCorner = OverlayButtonCorner.TopLeft;
        [SerializeField] private Vector2 overlayButtonPadding = new Vector2(10f, 10f);

        private string defaultCategory = string.Empty;
        private string defaultUnit = string.Empty;

        private readonly List<InteractiveCatalogEntry> availableInteractives = new List<InteractiveCatalogEntry>();
        private Coroutine catalogLoadRoutine;
        private bool gameLoadInProgress;

        public IReadOnlyList<InteractiveCatalogEntry> AvailableInteractives => availableInteractives;
        public event Action<IReadOnlyList<InteractiveCatalogEntry>> CatalogUpdated;
        public event Action<string> CatalogLoadFailed;

        /// <summary>
        /// Fired when the player taps an interactive that is not cached and there is no internet.
        /// Parameters: message string, the catalog entry (may be null if not found).
        /// </summary>
        public event Action<string, InteractiveCatalogEntry> GameLoadOfflineBlocked;
        public event Action<string, InteractiveCatalogEntry> GameLoadStarted;
        public event Action GameLoadFinished;

        private void Start()
        {
            EnsureCacheVersionCurrent();
            
            if (refreshCatalogOnStart)
            {
                RefreshCatalog();
            }
        }

        /// <summary>
        /// Sets the grade for catalog filtering (e.g., "Grade 1", "Grade2", "grade-3").
        /// </summary>
        public void SetGrade(string newGrade)
        {
            if (string.IsNullOrWhiteSpace(newGrade))
            {
                return;
            }

            string normalized = newGrade.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
            if (Enum.TryParse(normalized, true, out GradeLevel parsedGrade))
            {
                grade = parsedGrade;
                Debug.Log("[InteractiveController] Grade set to: " + InteractivePathResolver.GetGradePathName(grade));
            }
            else
            {
                Debug.LogWarning("[InteractiveController] Unknown grade: " + newGrade + ". Keeping current grade: " + InteractivePathResolver.GetGradePathName(grade));
            }
        }

        /// <summary>
        /// Sets the active TEK and refreshes catalog.
        /// </summary>
        public void SetTekAndRefresh(string tekName)
        {
            if (string.IsNullOrWhiteSpace(tekName))
            {
                return;
            }

            if (Enum.TryParse(tekName.Trim(), true, out CurrentTek parsedTek))
            {
                currentTek = parsedTek;
            }
            else
            {
                Debug.LogWarning("[InteractiveController] Unknown TEK: " + tekName + ". Keeping current TEK: " + InteractivePathResolver.GetTekPathName(currentTek));
                return;
            }

            Debug.Log("[InteractiveController] TEK set to: " + InteractivePathResolver.GetTekPathName(currentTek));
            RefreshCatalog();
        }

        public void RefreshCatalog()
        {
            if (catalogLoadRoutine != null)
            {
                StopCoroutine(catalogLoadRoutine);
            }

            string catalogUrl = InteractivePathResolver.BuildCatalogUrl(serverRoot, useGoogleSheetCatalogs, webAppUrl, currentTek, grade, catalogFileName);
            string cachePath = InteractiveCatalogService.GetCatalogCachePath(useGoogleSheetCatalogs, currentTek, grade, catalogFileName);

            catalogLoadRoutine = StartCoroutine(InteractiveCatalogService.LoadCatalogRoutine(
                catalogUrl,
                cachePath,
                availableInteractives,
                interactives => CatalogUpdated?.Invoke(interactives),
                message => CatalogLoadFailed?.Invoke(message),
                () => catalogLoadRoutine = null));
        }

        public void RequestGameLoad(string gameId)
        {
            if (gameLoadInProgress)
            {
                Debug.LogWarning("[InteractiveController] Ignoring RequestGameLoad - a load is already in progress.");
                return;
            }

            InteractiveCatalogEntry matchedEntry = FindCatalogEntry(gameId);
            bool isCached = IsInteractiveCached(gameId);
            string title = matchedEntry != null && !string.IsNullOrWhiteSpace(matchedEntry.title) ? matchedEntry.title : gameId;
            string loadMsg = isCached ? ("Loading " + title + "...") : ("Downloading " + title + "...");

            gameLoadInProgress = true;
            GameLoadStarted?.Invoke(loadMsg, matchedEntry);

            InteractiveBundleService.DownloadTarget target = BuildDownloadTarget(gameId, matchedEntry);
            StartCoroutine(InteractiveBundleService.DownloadAndStartRoutine(
                target,
                matchedEntry,
                folderName => InteractivePathResolver.BuildFolderUrl(serverRoot, currentTek, folderName),
                id => IsInteractiveCached(id),
                (message, entry) => GameLoadOfflineBlocked?.Invoke(message, entry),
                NotifyGameLoadFinished,
                overlayPrefab,
                overlayButtonCorner,
                overlayButtonPadding));
        }

        /// <summary>
        /// Returns true when both bundle files for the given game ID are already on disk.
        /// Use this to show a "Downloaded" badge on catalog buttons.
        /// </summary>
        public bool IsInteractiveCached(string gameId)
        {
            InteractiveBundleService.DownloadTarget target = BuildDownloadTarget(gameId, FindCatalogEntry(gameId));
            string dir = InteractiveBundleService.GetCacheDirectory(target.cacheKey);
            return File.Exists(Path.Combine(dir, target.bundleFileNameBase + ".assets"))
                && File.Exists(Path.Combine(dir, target.bundleFileNameBase + ".scenes"));
        }

        /// <summary>
        /// Returns true if the catalog JSON has been saved to disk at least once.
        /// Used by CatalogStatusOverlay to detect first-launch vs returning-launch.
        /// </summary>
        public bool IsCatalogCached()
        {
            string cachePath = InteractiveCatalogService.GetCatalogCachePath(useGoogleSheetCatalogs, currentTek, grade, catalogFileName);
            return InteractiveCatalogService.IsCatalogCached(cachePath);
        }

        public string ResolveCatalogAssetUrl(InteractiveCatalogEntry entry, string assetPath)
        {
            return InteractivePathResolver.ResolveCatalogAssetUrl(serverRoot, currentTek, grade, entry, assetPath);
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
    
        private InteractiveBundleService.DownloadTarget BuildDownloadTarget(string gameId, InteractiveCatalogEntry entry)
        {
            return InteractiveBundleService.BuildDownloadTarget(gameId, entry, grade, defaultCategory, defaultUnit, currentTek, bundlePrefix);
        }

        private void NotifyGameLoadFinished()
        {
            gameLoadInProgress = false;
            GameLoadFinished?.Invoke();
        }

        public string CurrentTekGetter()
        {
            return InteractivePathResolver.GetTekPathName(currentTek);
        }

        private void EnsureCacheVersionCurrent()
        {
            string currentVersion = string.IsNullOrWhiteSpace(Application.version) ? "0" : Application.version.Trim();
            string cachedVersion = PlayerPrefs.GetString(CacheVersionPlayerPrefsKey, string.Empty);

            if (string.Equals(cachedVersion, currentVersion, StringComparison.Ordinal))
            {
                return;
            }

            TryDeleteDirectory(Path.Combine(Application.persistentDataPath, "CatalogCache"));
            TryDeleteDirectory(Path.Combine(Application.persistentDataPath, "InteractiveCache"));
            TryDeleteDirectory(Path.Combine(Application.persistentDataPath, "ThumbnailCache"));

            PlayerPrefs.SetString(CacheVersionPlayerPrefsKey, currentVersion);
            PlayerPrefs.Save();
            Debug.Log("[Cache] Reset persistent cache for app version " + currentVersion + ".");
        }

        private void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch (Exception deleteEx)
            {
                Debug.LogWarning("Failed to delete cache directory: " + directoryPath + " | " + deleteEx.Message);
            }
        }
    }
}
