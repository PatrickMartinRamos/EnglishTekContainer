using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishTek.Core
{
    public class InteractiveCatalogMenu : MonoBehaviour
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
        private readonly List<GameObject> generatedCategoryButtons = new List<GameObject>();
        private readonly List<GameObject> generatedUnitButtons = new List<GameObject>();
        private readonly List<GameObject> generatedEntryButtons = new List<GameObject>();
        private readonly List<CategoryButtonBinding> categoryButtonBindings = new List<CategoryButtonBinding>();

        private bool homeBackgroundEnabled = false;

        private string selectedCategory;
        private string selectedUnit;
        private string pendingCategorySelection;

        private class CategoryButtonBinding
        {
            public string category;
            public Image background;
            public Text label;
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
            string normalizedUnit = CatalogStringHelper.NormalizeUnit(unitName);
            if (string.IsNullOrEmpty(normalizedUnit))
            {
                normalizedUnit = "general";
            }

            homeBackgroundEnabled = true;
            selectedUnit = normalizedUnit;
            OnUnitSelected();
            RenderFilteredEntries();
        }

        protected virtual void OnUnitSelected() { }

        protected void SetUnitButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < generatedUnitButtons.Count; i++)
            {
                if (generatedUnitButtons[i] == null) { continue; }
                Button btn = generatedUnitButtons[i].GetComponent<Button>();
                if (btn == null) { btn = generatedUnitButtons[i].GetComponentInChildren<Button>(true); }
                if (btn != null) { btn.interactable = interactable; }
            }
        }

        public virtual void GoBack() { }

        protected void HideHomeBackground()
        {
            homeBackgroundEnabled = false;
            if (entryHomeBackground != null) { entryHomeBackground.HideBackground(); }
        }

        public void SelectGrammarLesson()
        {
            SelectLesson("grammar");
        }

        public void SelectReadingLesson()
        {
            SelectLesson("reading");
        }

        public void SelectListeningLesson()
        {
            SelectLesson("listening");
        }

        public void SelectVirtualDialogueLesson()
        {
            SelectLesson("virtual dialogue");
        }

        private void OnDisable()
        {
            if (thumbnailLoader != null)
            {
                thumbnailLoader.StopAll();
            }

            if (controller == null)
            {
                return;
            }

            controller.CatalogUpdated -= HandleCatalogUpdated;
            controller.CatalogLoadFailed -= HandleCatalogLoadFailed;
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
                return;
            }

            for (int index = 0; index < interactives.Count; index++)
            {
                if (interactives[index] != null)
                {
                    cachedInteractives.Add(interactives[index]);
                }
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

            int addedCount = 0;
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
                addedCount++;
            }

            if (entryHomeBackground != null && homeBackgroundEnabled)
            {
                entryHomeBackground.SetEntries(renderedEntries);
            }

            if (addedCount == 0)
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

        private void CreateCategoryButtons(IReadOnlyList<string> categories)
        {
            if (categoryRowRect == null)
            {
                return;
            }

            for (int index = 0; index < categories.Count; index++)
            {
                string category = categories[index];
                GameObject buttonObject = new GameObject(category + "_CategoryButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonObject.transform.SetParent(categoryRowRect, false);

                Image bg = buttonObject.GetComponent<Image>();
                bg.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);

                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = bg;
                buttonObject.GetComponent<LayoutElement>().minHeight = 30f;

                Text label = CatalogUiFactory.CreateTextElement("Label", buttonObject.transform, 16f);
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.14f, 0.14f, 0.14f, 1f);
                label.text = CatalogStringHelper.FormatCategoryLabel(category);

                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 0f);
                labelRect.offsetMax = new Vector2(-8f, 0f);

                string selected = category;
                button.onClick.AddListener(() => ApplyCategoryFilter(selected));

                generatedCategoryButtons.Add(buttonObject);
                categoryButtonBindings.Add(new CategoryButtonBinding { category = category, background = bg, label = label });
            }
        }

        private void CreateUnitButtons(IReadOnlyList<string> units)
        {
            ClearUnitButtons();

            if (unitButtonContainer == null || unitButtonPrefab == null)
            {
                return;
            }

            for (int index = 0; index < units.Count; index++)
            {
                string unit = units[index];
                GameObject buttonObject = Instantiate(unitButtonPrefab, unitButtonContainer, false);
                buttonObject.name = unit + "_UnitButton";

                Button button = buttonObject.GetComponent<Button>();
                if (button == null)
                {
                    button = buttonObject.GetComponentInChildren<Button>(true);
                }

                if (button != null)
                {
                    button.interactable = true;
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.raycastTarget = true;
                    }
                    string selected = unit;
                    button.onClick.AddListener(() => SelectUnit(selected));
                }

                SetButtonLabel(buttonObject, CatalogStringHelper.FormatUnitLabel(unit));
                generatedUnitButtons.Add(buttonObject);
            }
        }

        private void CreateButton(InteractiveCatalogEntry entry)
        {
            if (entriesRect == null)
            {
                return;
            }

            GameObject buttonObject = new GameObject(entry.id + "_CatalogButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(entriesRect, false);
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 36f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => controller.RequestGameLoad(entry.id));
            buttonObject.GetComponent<LayoutElement>().minHeight = 36f;

            RawImage thumbnail = CatalogUiFactory.CreateThumbnailElement(buttonObject.transform);

            Text label = CatalogUiFactory.CreateTextElement("Label", buttonObject.transform, 22f);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            label.text = entry.DisplayName;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(52f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            if (thumbnailLoader != null)
            {
                thumbnailLoader.TryLoadThumbnail(entry, thumbnail, controller);
            }

            generatedEntryButtons.Add(buttonObject);
        }

        private void CreateInteractiveButtonFromPrefab(InteractiveCatalogEntry entry)
        {
            if (interactiveButtonContainer == null || interactiveButtonPrefab == null)
            {
                return;
            }

            GameObject buttonObject = Instantiate(interactiveButtonPrefab, interactiveButtonContainer, false);
            buttonObject.name = entry.id + "_InteractiveButton";

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.GetComponentInChildren<Button>(true);
            }

            if (button != null)
            {
                button.interactable = true;
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.raycastTarget = true;
                }
                button.onClick.AddListener(() => controller.RequestGameLoad(entry.id));
            }

            SetButtonLabel(buttonObject, entry.DisplayName);

            RawImage thumbnail = buttonObject.GetComponentInChildren<RawImage>(true);
            if (thumbnail != null && thumbnailLoader != null)
            {
                thumbnailLoader.TryLoadThumbnail(entry, thumbnail, controller);
            }

            generatedEntryButtons.Add(buttonObject);
        }

        private static void SetButtonLabel(GameObject buttonObject, string label)
        {
            if (buttonObject == null)
            {
                return;
            }

            TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                tmpText.text = label;
                return;
            }

            Text uiText = buttonObject.GetComponentInChildren<Text>(true);
            if (uiText != null)
            {
                uiText.text = label;
            }
        }

        private void UpdateCategoryButtonVisuals()
        {
            for (int index = 0; index < categoryButtonBindings.Count; index++)
            {
                CategoryButtonBinding binding = categoryButtonBindings[index];
                bool isSelected = string.Equals(binding.category, selectedCategory, StringComparison.OrdinalIgnoreCase);

                if (binding.background != null)
                {
                    binding.background.color = isSelected
                        ? new Color(0.22f, 0.56f, 0.92f, 0.95f)
                        : new Color(0.95f, 0.95f, 0.95f, 0.95f);
                }

                if (binding.label != null)
                {
                    binding.label.color = isSelected
                        ? Color.white
                        : new Color(0.14f, 0.14f, 0.14f, 1f);
                }
            }
        }

        private void ClearCategoryButtons()
        {
            for (int index = 0; index < generatedCategoryButtons.Count; index++)
            {
                if (generatedCategoryButtons[index] != null)
                {
                    Destroy(generatedCategoryButtons[index]);
                }
            }

            generatedCategoryButtons.Clear();
            categoryButtonBindings.Clear();
        }

        private void ClearUnitButtons()
        {
            for (int index = 0; index < generatedUnitButtons.Count; index++)
            {
                if (generatedUnitButtons[index] != null)
                {
                    Destroy(generatedUnitButtons[index]);
                }
            }

            generatedUnitButtons.Clear();
        }

        private void ClearEntryButtons()
        {
            if (thumbnailLoader != null)
            {
                thumbnailLoader.StopAll();
            }

            for (int index = 0; index < generatedEntryButtons.Count; index++)
            {
                if (generatedEntryButtons[index] != null)
                {
                    Destroy(generatedEntryButtons[index]);
                }
            }

            generatedEntryButtons.Clear();

            if (thumbnailLoader != null)
            {
                thumbnailLoader.ClearTextures();
            }
        }

        private void SetStatus(string message)
        {
            if (!showBuiltInCatalogPanel)
            {
                return;
            }

            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}

