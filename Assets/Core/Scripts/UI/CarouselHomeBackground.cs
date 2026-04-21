using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tek.Core
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
        [SerializeField] private Image backgroundImage;
        [SerializeField] private InteractiveController controller;
        [Tooltip("The UIGroup that shows the entry panel. When assigned, loading triggers after the show animation completes instead of while the panel may still be hidden.")]
        [SerializeField] private UIGroup entryGroup;

        private readonly List<InteractiveCatalogEntry> entries = new List<InteractiveCatalogEntry>();
        private Texture2D currentTexture;
        private Sprite currentSprite;

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
                carousel.OnCenterIndexChanged += HandleCenterChanged;

            if (entryGroup != null)
                entryGroup.OnShown += HandleEntryGroupShown;
        }

        private void OnDisable()
        {
            if (carousel != null)
                carousel.OnCenterIndexChanged -= HandleCenterChanged;

            if (entryGroup != null)
                entryGroup.OnShown -= HandleEntryGroupShown;
        }

        private void OnDestroy()
        {
            if (currentSprite != null) Destroy(currentSprite);
            if (currentTexture != null) Destroy(currentTexture);
        }

        /// <summary>Hide and clear the background image (e.g. when returning to category view).</summary>
        public void HideBackground()
        {
            if (backgroundImage != null)
                backgroundImage.color = new Color(1f, 1f, 1f, 0f);
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

            // Trigger immediately if no UIGroup is assigned (fallback / editor use)
            // OR if the entry panel is already visible — OnShown won't fire again for this cycle.
            // Otherwise wait for OnShown so we don't load while the panel is still animating in.
            if (entryGroup == null || entryGroup.IsVisible)
            {
                HandleCenterChanged(carousel != null ? carousel.CurrentCenterIndex : 0);
            }
        }

        // Fired by UIGroup.OnShown once the entry panel's show animation completes.
        private void HandleEntryGroupShown()
        {
            HandleCenterChanged(carousel != null ? carousel.CurrentCenterIndex : 0);
        }

        private void HandleCenterChanged(int index)
        {
            if (index < 0 || index >= entries.Count || controller == null) return;
            InteractiveCatalogEntry entry = entries[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.home)) return;

            string url = controller.ResolveCatalogAssetUrl(entry, entry.home);
            Texture2D tex = CatalogThumbnailLoader.ReadFromCache(url);
            if (tex != null) ApplyTexture(tex);
        }

        private void ApplyTexture(Texture2D tex)
        {
            if (currentSprite != null) Destroy(currentSprite);
            if (currentTexture != null) Destroy(currentTexture);

            currentTexture = tex;

            if (backgroundImage != null)
            {
                currentSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                backgroundImage.sprite = currentSprite;
                backgroundImage.color = Color.white;
            }
        }
    }
}