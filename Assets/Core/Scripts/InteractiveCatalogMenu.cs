using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
        [SerializeField] private Transform unitButtonContainer;
        [SerializeField] private GameObject unitButtonPrefab;
        [SerializeField] private Transform interactiveButtonContainer;
        [SerializeField] private GameObject interactiveButtonPrefab;
        [SerializeField] private bool autoSelectFirstUnit = true;
        [SerializeField] private Vector2 panelSize = new Vector2(360f, 220f);
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -200f);

        private RectTransform panelRect;
        private RectTransform categoryRowRect;
        private RectTransform entriesRect;
        private Text statusText;
        private readonly List<InteractiveCatalogEntry> cachedInteractives = new List<InteractiveCatalogEntry>();
        private readonly List<GameObject> generatedCategoryButtons = new List<GameObject>();
        private readonly List<GameObject> generatedUnitButtons = new List<GameObject>();
        private readonly List<GameObject> generatedEntryButtons = new List<GameObject>();
        private readonly List<CategoryButtonBinding> categoryButtonBindings = new List<CategoryButtonBinding>();
        private readonly List<Coroutine> imageLoadRoutines = new List<Coroutine>();
        private readonly List<Texture2D> loadedTextures = new List<Texture2D>();
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
            string normalizedCategory = NormalizeCategory(lessonCategory);
            if (string.IsNullOrEmpty(normalizedCategory))
            {
                normalizedCategory = "general";
            }

            if (!HasCategory(normalizedCategory))
            {
                pendingCategorySelection = normalizedCategory;
                SetStatus("Waiting for " + FormatCategoryLabel(normalizedCategory) + " interactives...");
                return;
            }

            pendingCategorySelection = null;
            ApplyCategoryFilter(normalizedCategory);
        }

        public void SelectUnit(string unitName)
        {
            string normalizedUnit = NormalizeUnit(unitName);
            if (string.IsNullOrEmpty(normalizedUnit))
            {
                normalizedUnit = "general";
            }

            selectedUnit = normalizedUnit;
            RenderFilteredEntries();
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
            StopAllImageLoadRoutines();

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

            statusText = CreateTextElement("CatalogStatus", panelObject.transform, 24f);
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.text = "Waiting for catalog...";

            categoryRowRect = CreateCategoryRow(panelObject.transform);
            entriesRect = CreateEntriesContainer(panelObject.transform);
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

            List<string> categories = BuildUniqueCategories(cachedInteractives);
            if (autoGenerateLessonButtons)
            {
                CreateCategoryButtons(categories);
            }

            if (!string.IsNullOrEmpty(pendingCategorySelection) && HasCategory(pendingCategorySelection))
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

        private void CreateButton(InteractiveCatalogEntry entry)
        {
            if (entriesRect == null)
            {
                return;
            }

            GameObject buttonObject = new GameObject(entry.id + "_CatalogButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(entriesRect, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 36f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => controller.RequestGameLoad(entry.id));

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = 36f;

            RawImage thumbnail = CreateThumbnailElement(buttonObject.transform);

            Text label = CreateTextElement("Label", buttonObject.transform, 22f);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            label.text = entry.DisplayName;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(52f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);

            TryLoadThumbnail(entry, thumbnail);

            generatedEntryButtons.Add(buttonObject);
        }

        private RectTransform CreateCategoryRow(Transform parent)
        {
            GameObject rowObject = new GameObject("CategoryRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 34f;

            return rowObject.GetComponent<RectTransform>();
        }

        private RectTransform CreateEntriesContainer(Transform parent)
        {
            GameObject entriesObject = new GameObject("CatalogEntries", typeof(RectTransform), typeof(VerticalLayoutGroup));
            entriesObject.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = entriesObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return entriesObject.GetComponent<RectTransform>();
        }

        private List<string> BuildUniqueCategories(IReadOnlyList<InteractiveCatalogEntry> interactives)
        {
            List<string> categories = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < interactives.Count; index++)
            {
                InteractiveCatalogEntry entry = interactives[index];
                string category = NormalizeCategory(entry != null ? entry.category : null);
                if (string.IsNullOrEmpty(category))
                {
                    category = "general";
                }

                if (seen.Add(category))
                {
                    categories.Add(category);
                }
            }

            return categories;
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

                Image background = buttonObject.GetComponent<Image>();
                background.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);

                Button button = buttonObject.GetComponent<Button>();
                button.targetGraphic = background;

                LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
                layout.minHeight = 30f;

                Text label = CreateTextElement("Label", buttonObject.transform, 16f);
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.14f, 0.14f, 0.14f, 1f);
                label.text = FormatCategoryLabel(category);

                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 0f);
                labelRect.offsetMax = new Vector2(-8f, 0f);

                string selected = category;
                button.onClick.AddListener(() => ApplyCategoryFilter(selected));

                generatedCategoryButtons.Add(buttonObject);
                categoryButtonBindings.Add(new CategoryButtonBinding
                {
                    category = category,
                    background = background,
                    label = label
                });
            }
        }

        private void ApplyCategoryFilter(string category)
        {
            selectedCategory = NormalizeCategory(category);
            selectedUnit = string.Empty;
            UpdateCategoryButtonVisuals();

            List<string> units = BuildUnitsForCategory(selectedCategory);
            CreateUnitButtons(units);

            if (autoSelectFirstUnit && units.Count > 0)
            {
                selectedUnit = units[0];
            }

            RenderFilteredEntries();
        }

        private bool HasCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            for (int index = 0; index < cachedInteractives.Count; index++)
            {
                InteractiveCatalogEntry entry = cachedInteractives[index];
                if (entry == null)
                {
                    continue;
                }

                string entryCategory = NormalizeCategory(entry.category);
                if (string.IsNullOrEmpty(entryCategory))
                {
                    entryCategory = "general";
                }

                if (string.Equals(entryCategory, category, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void RenderFilteredEntries()
        {
            ClearEntryButtons();

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

                string entryCategory = NormalizeCategory(entry.category);
                if (string.IsNullOrEmpty(entryCategory))
                {
                    entryCategory = "general";
                }

                if (!string.Equals(entryCategory, selectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string entryUnit = NormalizeUnit(entry.unit);
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
                addedCount++;
            }

            if (addedCount == 0)
            {
                if (string.IsNullOrEmpty(selectedUnit))
                {
                    SetStatus("No interactives found in " + FormatCategoryLabel(selectedCategory) + ".");
                }
                else
                {
                    SetStatus("No interactives found in " + FormatCategoryLabel(selectedCategory) + " / " + FormatUnitLabel(selectedUnit) + ".");
                }
                return;
            }

            if (string.IsNullOrEmpty(selectedUnit))
            {
                SetStatus(FormatCategoryLabel(selectedCategory) + " interactives");
            }
            else
            {
                SetStatus(FormatCategoryLabel(selectedCategory) + " / " + FormatUnitLabel(selectedUnit) + " interactives");
            }
        }

        private List<string> BuildUnitsForCategory(string category)
        {
            List<string> units = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(category))
            {
                return units;
            }

            for (int index = 0; index < cachedInteractives.Count; index++)
            {
                InteractiveCatalogEntry entry = cachedInteractives[index];
                if (entry == null)
                {
                    continue;
                }

                string entryCategory = NormalizeCategory(entry.category);
                if (string.IsNullOrEmpty(entryCategory))
                {
                    entryCategory = "general";
                }

                if (!string.Equals(entryCategory, category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string unit = NormalizeUnit(entry.unit);
                if (string.IsNullOrEmpty(unit))
                {
                    unit = "general";
                }

                if (seen.Add(unit))
                {
                    units.Add(unit);
                }
            }

            return units;
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
                    string selected = unit;
                    button.onClick.AddListener(() => SelectUnit(selected));
                }

                SetButtonLabel(buttonObject, FormatUnitLabel(unit));
                generatedUnitButtons.Add(buttonObject);
            }
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
                button.onClick.AddListener(() => controller.RequestGameLoad(entry.id));
            }

            SetButtonLabel(buttonObject, entry.DisplayName);

            RawImage thumbnail = buttonObject.GetComponentInChildren<RawImage>(true);
            if (thumbnail != null)
            {
                TryLoadThumbnail(entry, thumbnail);
            }

            generatedEntryButtons.Add(buttonObject);
        }

        private void SetButtonLabel(GameObject buttonObject, string label)
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

        private string NormalizeCategory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant();
        }

        private string NormalizeUnit(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant();
        }

        private string FormatCategoryLabel(string category)
        {
            string normalized = NormalizeCategory(category);
            if (string.IsNullOrEmpty(normalized))
            {
                return "General";
            }

            string[] words = normalized.Split(' ');
            for (int index = 0; index < words.Length; index++)
            {
                if (string.IsNullOrEmpty(words[index]))
                {
                    continue;
                }

                words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
            }

            return string.Join(" ", words);
        }

        private string FormatUnitLabel(string unit)
        {
            string normalized = NormalizeUnit(unit);
            if (string.IsNullOrEmpty(normalized))
            {
                return "General";
            }

            string[] words = normalized.Split(' ');
            for (int index = 0; index < words.Length; index++)
            {
                if (string.IsNullOrEmpty(words[index]))
                {
                    continue;
                }

                words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
            }

            return string.Join(" ", words);
        }

        private RawImage CreateThumbnailElement(Transform parent)
        {
            GameObject thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(RawImage));
            thumbnailObject.transform.SetParent(parent, false);

            RawImage thumbnail = thumbnailObject.GetComponent<RawImage>();
            thumbnail.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            RectTransform thumbnailRect = thumbnail.GetComponent<RectTransform>();
            thumbnailRect.anchorMin = new Vector2(0f, 0f);
            thumbnailRect.anchorMax = new Vector2(0f, 1f);
            thumbnailRect.pivot = new Vector2(0f, 0.5f);
            thumbnailRect.sizeDelta = new Vector2(40f, -8f);
            thumbnailRect.anchoredPosition = new Vector2(6f, 0f);

            return thumbnail;
        }

        private void TryLoadThumbnail(InteractiveCatalogEntry entry, RawImage target)
        {
            if (entry == null || target == null || controller == null || string.IsNullOrWhiteSpace(entry.image))
            {
                return;
            }

            string imageUrl = controller.ResolveCatalogAssetUrl(entry, entry.image);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            Coroutine routine = StartCoroutine(LoadThumbnailRoutine(imageUrl, target));
            imageLoadRoutines.Add(routine);
        }

        private IEnumerator LoadThumbnailRoutine(string imageUrl, RawImage target)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogWarning("Catalog thumbnail download failed: " + request.error + " | URL: " + imageUrl);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                yield break;
            }

            loadedTextures.Add(texture);

            if (target != null)
            {
                target.texture = texture;
                target.color = Color.white;
            }
        }

        private Text CreateTextElement(string objectName, Transform parent, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0f, 28f);

            return text;
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
            StopAllImageLoadRoutines();

            for (int index = 0; index < generatedEntryButtons.Count; index++)
            {
                if (generatedEntryButtons[index] != null)
                {
                    Destroy(generatedEntryButtons[index]);
                }
            }

            generatedEntryButtons.Clear();

            for (int index = 0; index < loadedTextures.Count; index++)
            {
                if (loadedTextures[index] != null)
                {
                    Destroy(loadedTextures[index]);
                }
            }

            loadedTextures.Clear();
        }

        private void StopAllImageLoadRoutines()
        {
            for (int index = 0; index < imageLoadRoutines.Count; index++)
            {
                if (imageLoadRoutines[index] != null)
                {
                    StopCoroutine(imageLoadRoutines[index]);
                }
            }

            imageLoadRoutines.Clear();
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