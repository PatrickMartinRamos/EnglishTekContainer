using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tek.Core
{
    /// <summary>
    /// Single loading screen that shows the right message based on launch context:
    ///
    ///   First launch ONLINE   → "Downloading Files..."  → hides after min time, menu appears.
    ///   First launch OFFLINE  → "Connect to Internet"   → stays visible; menu is blocked.
    ///   Return launch         → "Checking Files..."     → hides after min time, menu appears.
    ///
    /// Also handles offline-tap notifications: briefly shows the panel with a
    /// message like "Connect to the internet to download [title]" then hides.
    ///
    /// Setup (Inspector):
    ///   • loadingPanel  → UIGroup on your loading screen root (startHidden = false).
    ///   • loadingLabel  → TextMeshProUGUI for the status text.
    ///   • loadingImage  → Image for your intro/loading graphic (optional).
    ///   • controller    → auto-found if left empty.
    /// </summary>
    [DisallowMultipleComponent]
    public class CatalogStatusOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The InteractiveController in the scene. Auto-found if not assigned.")]
        [SerializeField] private InteractiveController controller = null;

        [Tooltip("The InteractiveCatalogMenu (or CatalogMenuNavigator) in the scene. When assigned, the loading screen stays open until all images are downloaded.")]
        [SerializeField] private InteractiveCatalogMenu catalogMenu = null;

        [Tooltip("UIGroup on the loading screen root.")]
        [SerializeField] private UIGroup loadingPanel = null;

        [Tooltip("TextMeshProUGUI that displays the status message.")]
        [SerializeField] private TextMeshProUGUI loadingLabel = null;

        [Tooltip("Background Image in the offline notification panel. Replaced with the interactive's cached home image when shown.")]
        [SerializeField] private Image notificationBackground = null;

        [Header("Messages")]
        [Tooltip("Shown on first launch while downloading catalog. Online + no cache.")]
        [SerializeField] private string msgDownloading = "Downloading Files...";

        [Tooltip("Shown on return launches while verifying catalog. Cache exists.")]
        [SerializeField] private string msgChecking = "Checking Files...";

        [Tooltip("Shown when launched offline with no local cache. Screen stays until app is restarted with internet.")]
        [SerializeField] private string msgNoInternet = "Connect to the Internet to continue.";

        [Header("Timing")]
        [Tooltip("Minimum seconds the loading screen is shown before hiding. Acts as intro duration.")]
        [SerializeField] private float minimumDisplaySeconds = 3f;

        [Tooltip("Seconds the notification message is shown when a player taps an uncached interactive offline. 0 = no auto-hide.")]
        [SerializeField] private float notificationDismissSeconds = 4f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private float shownAt;
        private bool isFirstLaunchOffline;   // stuck state — do not hide
        private bool catalogDone;            // catalog succeeded or loaded from cache
        private bool imagesReady;            // ImagesReady fired (may arrive before catalogDone)
        private Coroutine notificationRoutine;
        private string messageBeforeNotification;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindObjectOfType<InteractiveController>();
            }
        }

        private void OnEnable()
        {
            if (controller == null) return;
            controller.CatalogUpdated        += OnCatalogUpdated;
            controller.CatalogLoadFailed     += OnCatalogLoadFailed;
            controller.GameLoadOfflineBlocked += OnGameLoadOfflineBlocked;
            if (catalogMenu != null) catalogMenu.ImagesReady += OnImagesReady;
        }

        private void OnDisable()
        {
            if (controller == null) return;
            controller.CatalogUpdated        -= OnCatalogUpdated;
            controller.CatalogLoadFailed     -= OnCatalogLoadFailed;
            controller.GameLoadOfflineBlocked -= OnGameLoadOfflineBlocked;
            if (catalogMenu != null) catalogMenu.ImagesReady -= OnImagesReady;
        }

        private void Start()
        {
            shownAt = Time.realtimeSinceStartup;
            catalogDone = false;
            isFirstLaunchOffline = false;

            bool hasCacheOnDisk = controller != null && controller.IsCatalogCached();
            SetLabel(hasCacheOnDisk ? msgChecking : msgDownloading);

            if (loadingPanel != null)
            {
                loadingPanel.Show();
            }

            // If the controller loaded the catalog before we could subscribe to its event
            // (e.g. synchronous cache load in Awake), catch up now so catalogDone is correct.
            if (controller != null && controller.AvailableInteractives.Count > 0)
            {
                catalogDone = true;
                if (catalogMenu != null && !imagesReady)
                    SetLabel("Preparing Images...");
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnCatalogUpdated(System.Collections.Generic.IReadOnlyList<InteractiveCatalogEntry> entries)
        {
            isFirstLaunchOffline = false;
            catalogDone = true;
            // If catalogMenu is assigned, wait for ImagesReady before hiding.
            // If ImagesReady already fired before us (race), hide immediately.
            if (catalogMenu == null || imagesReady)
            {
                HideAfterMinTime();
            }
            else
            {
                SetLabel("Preparing Images...");
            }
        }

        private void OnImagesReady()
        {
            imagesReady = true;
            // ImagesReady only fires once the catalog has loaded and all downloads are done.
            // It is always safe to hide here — no need to wait for catalogDone.
            if (isFirstLaunchOffline) return;
            HideAfterMinTime();
        }

        private void OnCatalogLoadFailed(string error)
        {
            // CatalogLoadFailed only fires when there is no local cache AND the network
            // request failed. That is the true "no internet, first launch" state.
            bool hasCacheOnDisk = controller != null && controller.IsCatalogCached();
            if (!hasCacheOnDisk)
            {
                // Genuinely stuck — no cache, no internet.
                isFirstLaunchOffline = true;
                SetLabel(msgNoInternet);
                return;
            }

            // Cache exists but something else failed (empty JSON, parse error).
            // The cached catalog was already used, so just hide.
            catalogDone = true;
            HideAfterMinTime();
        }

        private void OnGameLoadOfflineBlocked(string message, InteractiveCatalogEntry entry)
        {
            ShowNotification(message, entry);
        }

        // ── Notification (brief overlay message) ──────────────────────────────

        private void ShowNotification(string message, InteractiveCatalogEntry entry = null)
        {
            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
            }

            notificationRoutine = StartCoroutine(NotificationRoutine(message, entry));
        }

        private IEnumerator NotificationRoutine(string message, InteractiveCatalogEntry entry)
        {
            SetLabel(message);

            // Try to show the cached home image on the notification background.
            Sprite originalSprite = null;
            bool swappedImage = false;
            if (notificationBackground != null && entry != null)
            {
                string homeUrl = null;
                if (!string.IsNullOrWhiteSpace(entry.home))
                    homeUrl = controller != null ? controller.ResolveCatalogAssetUrl(entry, entry.home) : null;
                else if (!string.IsNullOrWhiteSpace(entry.image))
                    homeUrl = controller != null ? controller.ResolveCatalogAssetUrl(entry, entry.image) : null;

                if (!string.IsNullOrWhiteSpace(homeUrl))
                {
                    string cachePath = CatalogThumbnailLoader.GetImageCachePath(homeUrl);
                    if (File.Exists(cachePath))
                    {
                        byte[] bytes = null;
                        try { bytes = File.ReadAllBytes(cachePath); } catch { }
                        if (bytes != null && bytes.Length > 0)
                        {
                            Texture2D tex = new Texture2D(2, 2);
                            if (tex.LoadImage(bytes))
                            {
                                originalSprite = notificationBackground.sprite;
                                notificationBackground.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                                notificationBackground.color = Color.white;
                                swappedImage = true;
                            }
                            else
                            {
                                Destroy(tex);
                            }
                        }
                    }
                }
            }

            if (loadingPanel != null && !loadingPanel.IsVisible)
            {
                loadingPanel.Show();
            }

            if (notificationDismissSeconds > 0f)
            {
                yield return new WaitForSeconds(notificationDismissSeconds);

                // Restore original background before hiding.
                if (swappedImage && notificationBackground != null)
                {
                    if (notificationBackground.sprite != null && notificationBackground.sprite != originalSprite)
                    {
                        Destroy(notificationBackground.sprite.texture);
                        Destroy(notificationBackground.sprite);
                    }
                    notificationBackground.sprite = originalSprite;
                }

                if (catalogDone && !isFirstLaunchOffline && loadingPanel != null)
                {
                    loadingPanel.Hide();
                }
            }

            notificationRoutine = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void HideAfterMinTime()
        {
            float elapsed = Time.realtimeSinceStartup - shownAt;
            float remaining = minimumDisplaySeconds - elapsed;
            if (remaining > 0f)
            {
                StartCoroutine(HideAfterDelay(remaining));
            }
            else
            {
                if (loadingPanel != null) loadingPanel.Hide();
            }
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (loadingPanel != null) loadingPanel.Hide();
        }

        private void SetLabel(string text)
        {
            if (loadingLabel != null)
            {
                loadingLabel.text = text;
            }
        }
    }
}
