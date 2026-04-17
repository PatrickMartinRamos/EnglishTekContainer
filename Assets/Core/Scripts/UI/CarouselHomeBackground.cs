using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace EnglishTek.Core
{
    /// <summary>
    /// Watches an ArcCarousel and swaps a background RawImage to the centered
    /// entry's "home" image (served from the same location as catalog assets).
    ///
    /// Setup:
    ///   1. Add this component anywhere convenient (e.g. the entry group root).
    ///   2. Assign the ArcCarousel that holds the entry buttons.
    ///   3. Assign the RawImage that acts as the full-bleed home background.
    ///   4. Assign the InteractiveController so asset URLs are resolved correctly.
    ///   5. InteractiveCatalogMenu will call SetEntries() automatically after
    ///      it renders the entry buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarouselHomeBackground : MonoBehaviour
    {
        [SerializeField] private ArcCarousel carousel;
        [SerializeField] private RawImage backgroundImage;
        [SerializeField] private InteractiveController controller;

        private readonly List<InteractiveCatalogEntry> entries = new List<InteractiveCatalogEntry>();
        private Coroutine loadRoutine;
        private Texture2D currentTexture;

        private void Awake()
        {
            // Start transparent — only show once a home image is actually loaded.
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void OnEnable()
        {
            if (carousel != null)
            {
                carousel.OnCenterIndexChanged += HandleCenterChanged;
            }
        }

        private void OnDisable()
        {
            if (carousel != null)
            {
                carousel.OnCenterIndexChanged -= HandleCenterChanged;
            }
        }

        private void OnDestroy()
        {
            if (currentTexture != null)
            {
                Destroy(currentTexture);
            }
        }

        /// <summary>Hide and clear the background image (e.g. when returning to category view).</summary>
        public void HideBackground()
        {
            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// Called by InteractiveCatalogMenu after it spawns the entry buttons.
        /// The order must match the carousel children order.
        /// </summary>
        public void SetEntries(IReadOnlyList<InteractiveCatalogEntry> newEntries)
        {
            entries.Clear();
            for (int i = 0; i < newEntries.Count; i++)
            {
                entries.Add(newEntries[i]);
            }

            HandleCenterChanged(carousel != null ? carousel.CurrentCenterIndex : 0);
        }

        private void HandleCenterChanged(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                return;
            }

            InteractiveCatalogEntry entry = entries[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.home))
            {
                return;
            }

            if (controller == null)
            {
                return;
            }

            string url = controller.ResolveCatalogAssetUrl(entry, entry.home);
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
            }

            loadRoutine = StartCoroutine(LoadHomeRoutine(url));
        }

        private IEnumerator LoadHomeRoutine(string url)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogWarning("CarouselHomeBackground: Failed to load home image: " + request.error + " | " + url);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(request);
            if (tex == null)
            {
                yield break;
            }

            if (currentTexture != null)
            {
                Destroy(currentTexture);
            }

            currentTexture = tex;

            if (backgroundImage != null)
            {
                backgroundImage.texture = tex;
                backgroundImage.color = Color.white;
            }
        }
    }
}
