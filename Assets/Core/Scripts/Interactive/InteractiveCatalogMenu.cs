using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tek.Core
{
    public partial class InteractiveCatalogMenu : MonoBehaviour
    {
        [SerializeField] private InteractiveController controller;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private bool refreshOnStart = true;
        [SerializeField] private bool showBuiltInCatalogPanel = false;
        [SerializeField] private bool autoGenerateLessonButtons = true;
        [SerializeField] private Transform unitButtonContainer = null;
        [SerializeField] private GameObject unitButtonPrefab = null;
        [SerializeField] private Transform interactiveButtonContainer = null;
        [SerializeField] private GameObject interactiveButtonPrefab;
        [SerializeField] private bool autoSelectFirstUnit = true;
        [SerializeField] private Vector2 panelSize = new Vector2(360f, 220f);
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -200f);
        [SerializeField] private CarouselHomeBackground entryHomeBackground = null;

        private RectTransform panelRect;
        private RectTransform categoryRowRect;
        private RectTransform entriesRect;
        private Text statusText;
        private CatalogThumbnailLoader thumbnailLoader;

        private readonly List<InteractiveCatalogEntry> cachedInteractives = new List<InteractiveCatalogEntry>();
        private readonly List<InteractiveCatalogEntry> renderedEntries = new List<InteractiveCatalogEntry>();
        private readonly List<string> renderedUnits = new List<string>();
        private readonly List<GameObject> generatedCategoryButtons = new List<GameObject>();
        private readonly List<GameObject> generatedUnitButtons = new List<GameObject>();
        private readonly List<GameObject> generatedEntryButtons = new List<GameObject>();
        private readonly List<CategoryButtonBinding> categoryButtonBindings = new List<CategoryButtonBinding>();
        private readonly List<UnitButtonBinding> unitButtonBindings = new List<UnitButtonBinding>();
        private readonly List<EntryButtonBinding> entryButtonBindings = new List<EntryButtonBinding>();

        private bool homeBackgroundEnabled = false;
        private bool unitButtonsInteractable = true;
        private bool suppressUnitCarouselSelection = false;
        private ArcCarousel unitCarousel;
        private ArcCarousel entryCarousel;

        private string selectedCategory;
        private string selectedUnit;
        private string pendingCategorySelection;

        private class CategoryButtonBinding
        {
            public string category;
            public Image background;
            public Text label;
        }

        private class EntryButtonBinding
        {
            public GameObject root;
            public GameObject playObject;
            public Button playButton;
        }

        private class UnitButtonBinding
        {
            public GameObject root;
            public Button button;
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<InteractiveController>();
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }

            thumbnailLoader = GetComponent<CatalogThumbnailLoader>();
            if (thumbnailLoader == null)
            {
                thumbnailLoader = gameObject.AddComponent<CatalogThumbnailLoader>();
            }

            unitCarousel = ResolveUnitCarousel();
            entryCarousel = ResolveEntryCarousel();

            EnsureUi();
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                return;
            }

            controller.CatalogUpdated += HandleCatalogUpdated;
            controller.CatalogLoadFailed += HandleCatalogLoadFailed;

            unitCarousel = ResolveUnitCarousel();
            if (unitCarousel != null)
            {
                unitCarousel.OnCenterIndexChanged += HandleUnitCenterChanged;
            }

            entryCarousel = ResolveEntryCarousel();
            if (entryCarousel != null)
            {
                entryCarousel.OnCenterIndexChanged += HandleEntryCenterChanged;
            }
        }

        private void Start()
        {
            if (controller == null)
            {
                SetStatus("InteractiveController not found.");
                return;
            }

            if (controller.AvailableInteractives.Count > 0)
            {
                HandleCatalogUpdated(controller.AvailableInteractives);
                return;
            }

            if (refreshOnStart)
            {
                SetStatus("Loading interactives...");
                controller.RefreshCatalog();
            }
        }

        public void SelectLesson(string lessonCategory)
        {
            string normalizedCategory = CatalogStringHelper.NormalizeCategory(lessonCategory);
            if (string.IsNullOrEmpty(normalizedCategory))
            {
                normalizedCategory = "general";
            }

            if (!CatalogFilter.HasCategory(cachedInteractives, normalizedCategory))
            {
                pendingCategorySelection = normalizedCategory;
                SetStatus("Waiting for " + CatalogStringHelper.FormatCategoryLabel(normalizedCategory) + " interactives...");
                return;
            }

            pendingCategorySelection = null;
            ApplyCategoryFilter(normalizedCategory);
        }

        public void SelectUnit(string unitName)
        {
            ApplyUnitSelection(unitName, true);
        }

        protected virtual void OnUnitSelected() { }

        protected void SetUnitButtonsInteractable(bool interactable)
        {
            unitButtonsInteractable = interactable;
            UpdateUnitButtonsInteractable();
        }

        public virtual void GoBack() { }

        protected void HideHomeBackground()
        {
            homeBackgroundEnabled = false;
            if (entryHomeBackground != null) { entryHomeBackground.HideBackground(); }
        }

        private void OnDisable()
        {
            if (controller == null)
            {
                return;
            }

            controller.CatalogUpdated -= HandleCatalogUpdated;
            controller.CatalogLoadFailed -= HandleCatalogLoadFailed;

            if (unitCarousel != null)
            {
                unitCarousel.OnCenterIndexChanged -= HandleUnitCenterChanged;
            }

            if (entryCarousel != null)
            {
                entryCarousel.OnCenterIndexChanged -= HandleEntryCenterChanged;
            }
        }

        private void EnsureUi()
        {
            if (!showBuiltInCatalogPanel)
            {
                return;
            }

            if (panelRect != null || targetCanvas == null)
            {
                return;
            }

            GameObject panelObject = new GameObject("InteractiveCatalogPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelObject.transform.SetParent(targetCanvas.transform, false);

            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = anchoredPosition;

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.65f);

            VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            statusText = CatalogUiFactory.CreateTextElement("CatalogStatus", panelObject.transform, 24f);
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.text = "Waiting for catalog...";

            categoryRowRect = CatalogUiFactory.CreateCategoryRow(panelObject.transform);
            entriesRect = CatalogUiFactory.CreateEntriesContainer(panelObject.transform);
        }

        private void HandleCatalogUpdated(IReadOnlyList<InteractiveCatalogEntry> interactives)
        {
            EnsureUi();
            ClearCategoryButtons();
            ClearUnitButtons();
            ClearEntryButtons();
            cachedInteractives.Clear();
            selectedUnit = string.Empty;

            if (interactives == null || interactives.Count == 0)
            {
                SetStatus("No interactives found in catalog.");
                ImagesReady?.Invoke();
                return;
            }

            for (int index = 0; index < interactives.Count; index++)
            {
                if (interactives[index] != null)
                {
                    cachedInteractives.Add(interactives[index]);
                }
            }

            // Collect all image URLs to preload. Download finishes before the loading screen hides.
            if (thumbnailLoader != null)
            {
                List<string> urlsToPreload = new List<string>();
                for (int index = 0; index < cachedInteractives.Count; index++)
                {
                    InteractiveCatalogEntry e = cachedInteractives[index];
                    if (!string.IsNullOrWhiteSpace(e.home))
                        urlsToPreload.Add(controller.ResolveCatalogAssetUrl(e, e.home));
                    if (!string.IsNullOrWhiteSpace(e.image))
                        urlsToPreload.Add(controller.ResolveCatalogAssetUrl(e, e.image));
                }
                thumbnailLoader.DownloadAllAndNotify(urlsToPreload, () => ImagesReady?.Invoke());
            }
            else
            {
                ImagesReady?.Invoke();
            }

            List<string> categories = CatalogFilter.BuildUniqueCategories(cachedInteractives);
            if (autoGenerateLessonButtons)
            {
                CreateCategoryButtons(categories);
            }

            if (!string.IsNullOrEmpty(pendingCategorySelection) && CatalogFilter.HasCategory(cachedInteractives, pendingCategorySelection))
            {
                ApplyCategoryFilter(pendingCategorySelection);
                pendingCategorySelection = null;
            }
            else if (autoGenerateLessonButtons && categories.Count > 0)
            {
                ApplyCategoryFilter(categories[0]);
            }
            else
            {
                selectedCategory = string.Empty;
                selectedUnit = string.Empty;
                RenderFilteredEntries();
            }
        }

        /// <summary>
        /// Fired after the catalog has loaded AND all home/thumbnail images have been
        /// downloaded and cached to disk. Subscribe to this in CatalogStatusOverlay
        /// to hold the loading screen until images are ready.
        /// </summary>
        public event System.Action ImagesReady;

        private void HandleCatalogLoadFailed(string message)
        {
            EnsureUi();
            ClearCategoryButtons();
            ClearUnitButtons();
            ClearEntryButtons();
            cachedInteractives.Clear();
            selectedCategory = null;
            selectedUnit = string.Empty;
            SetStatus("Catalog unavailable. Manual ID buttons still work.");
            Debug.LogWarning(message);
        }

        private void ApplyCategoryFilter(string category)
        {
            homeBackgroundEnabled = false;
            if (entryHomeBackground != null) { entryHomeBackground.HideBackground(); }

            selectedCategory = CatalogStringHelper.NormalizeCategory(category);
            selectedUnit = string.Empty;
            UpdateCategoryButtonVisuals();

            List<string> units = CatalogFilter.BuildUnitsForCategory(cachedInteractives, selectedCategory);
            CreateUnitButtons(units);

            if (autoSelectFirstUnit && units.Count > 0)
            {
                selectedUnit = units[0];
            }

            OnCategoryApplied();
            RenderFilteredEntries();
        }

        protected virtual void OnCategoryApplied() { }

        private void RenderFilteredEntries()
        {
            ClearEntryButtons();
            renderedEntries.Clear();

            if (string.IsNullOrEmpty(selectedCategory))
            {
                return;
            }

            for (int index = 0; index < cachedInteractives.Count; index++)
            {
                InteractiveCatalogEntry entry = cachedInteractives[index];
                if (entry == null)
                {
                    continue;
                }

                string entryCategory = CatalogStringHelper.NormalizeCategory(entry.category);
                if (string.IsNullOrEmpty(entryCategory))
                {
                    entryCategory = "general";
                }

                if (!string.Equals(entryCategory, selectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string entryUnit = CatalogStringHelper.NormalizeUnit(entry.unit);
                if (string.IsNullOrEmpty(entryUnit))
                {
                    entryUnit = "general";
                }

                if (!string.IsNullOrEmpty(selectedUnit) && !string.Equals(entryUnit, selectedUnit, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (interactiveButtonContainer != null && interactiveButtonPrefab != null)
                {
                    CreateInteractiveButtonFromPrefab(entry);
                }
                else
                {
                    CreateButton(entry);
                }

                renderedEntries.Add(entry);
            }

            if (entryHomeBackground != null && homeBackgroundEnabled)
            {
                entryHomeBackground.SetEntries(renderedEntries);
            }

            UpdateEntryPlayButtons();

            if (renderedEntries.Count == 0)
            {
                if (string.IsNullOrEmpty(selectedUnit))
                {
                    SetStatus("No interactives found in " + CatalogStringHelper.FormatCategoryLabel(selectedCategory) + ".");
                }
                else
                {
                    SetStatus("No interactives found in " + CatalogStringHelper.FormatCategoryLabel(selectedCategory) + " / " + CatalogStringHelper.FormatUnitLabel(selectedUnit) + ".");
                }
                return;
            }

            if (string.IsNullOrEmpty(selectedUnit))
            {
                SetStatus(CatalogStringHelper.FormatCategoryLabel(selectedCategory) + " interactives");
            }
            else
            {
                SetStatus(CatalogStringHelper.FormatCategoryLabel(selectedCategory) + " / " + CatalogStringHelper.FormatUnitLabel(selectedUnit) + " interactives");
            }
        }

        private void HandleEntryCenterChanged(int index)
        {
            UpdateEntryPlayButtons(index);
        }

        private void HandleUnitCenterChanged(int index)
        {
            UpdateUnitButtonsInteractable(index);

            if (suppressUnitCarouselSelection || index < 0 || index >= renderedUnits.Count)
            {
                return;
            }

            ApplyUnitSelection(renderedUnits[index], false);
        }

        private void ApplyUnitSelection(string unitName, bool triggerNavigation)
        {
            string normalizedUnit = CatalogStringHelper.NormalizeUnit(unitName);
            if (string.IsNullOrEmpty(normalizedUnit))
            {
                normalizedUnit = "general";
            }

            bool hasChanged = !string.Equals(selectedUnit, normalizedUnit, StringComparison.OrdinalIgnoreCase);
            if (!hasChanged && !triggerNavigation)
            {
                return;
            }

            if (triggerNavigation)
            {
                homeBackgroundEnabled = true;
            }

            selectedUnit = normalizedUnit;

            if (triggerNavigation)
            {
                OnUnitSelected();
            }

            RenderFilteredEntries();
        }
    }
}

