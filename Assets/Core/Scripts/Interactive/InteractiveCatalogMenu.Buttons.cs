using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tek.Core
{
    public partial class InteractiveCatalogMenu
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Button Creation
        // ──────────────────────────────────────────────────────────────────────────

        private void CreateCategoryButtons(IReadOnlyList<string> categories)
        {
            if (categoryRowRect == null) return;

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
            renderedUnits.Clear();

            if (unitButtonContainer == null || unitButtonPrefab == null) return;

            suppressUnitCarouselSelection = true;

            for (int index = 0; index < units.Count; index++)
            {
                string unit = units[index];
                GameObject buttonObject = Instantiate(unitButtonPrefab, unitButtonContainer, false);
                buttonObject.name = unit + "_UnitButton";

                Button button = buttonObject.GetComponent<Button>();
                if (button == null)
                    button = buttonObject.GetComponentInChildren<Button>(true);

                if (button != null)
                {
                    button.interactable = true;
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage != null) buttonImage.raycastTarget = true;
                    string selected = unit;
                    button.onClick.AddListener(() => SelectUnit(selected));
                }

                SetButtonLabel(buttonObject, CatalogStringHelper.FormatUnitLabel(unit));
                generatedUnitButtons.Add(buttonObject);
                unitButtonBindings.Add(new UnitButtonBinding { root = buttonObject, button = button });
                renderedUnits.Add(unit);
            }

            suppressUnitCarouselSelection = false;

            if (unitCarousel == null)
                unitCarousel = ResolveUnitCarousel();

            if (unitCarousel != null)
                unitCarousel.Rebuild();

            UpdateUnitButtonsInteractable();
        }

        private void CreateButton(InteractiveCatalogEntry entry)
        {
            if (entriesRect == null) return;

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
                thumbnailLoader.LoadInto(controller.ResolveCatalogAssetUrl(entry, entry.image), thumbnail);

            generatedEntryButtons.Add(buttonObject);
        }

        private void CreateInteractiveButtonFromPrefab(InteractiveCatalogEntry entry)
        {
            if (interactiveButtonContainer == null || interactiveButtonPrefab == null) return;

            GameObject buttonObject = Instantiate(interactiveButtonPrefab, interactiveButtonContainer, false);
            buttonObject.name = entry.id + "_InteractiveButton";

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
                button = buttonObject.GetComponentInChildren<Button>(true);

            if (button != null)
            {
                button.interactable = true;
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null) buttonImage.raycastTarget = true;
                button.onClick.AddListener(() => controller.RequestGameLoad(entry.id));
            }

            SetButtonLabel(buttonObject, entry.DisplayName);
            entryButtonBindings.Add(CreateEntryButtonBinding(buttonObject, button));

            if (thumbnailLoader != null)
            {
                string thumbUrl = controller.ResolveCatalogAssetUrl(entry, entry.image);
                RawImage thumbnail = FindThumbnailRawImage(buttonObject);
                if (thumbnail != null)
                {
                    thumbnailLoader.LoadInto(thumbUrl, thumbnail);
                }
                else if (button != null)
                {
                    Image buttonImage = FindThumbnailImage(buttonObject, button);
                    if (buttonImage != null) thumbnailLoader.LoadInto(thumbUrl, buttonImage);
                }
            }

            generatedEntryButtons.Add(buttonObject);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Button State Updates
        // ──────────────────────────────────────────────────────────────────────────

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
                    binding.label.color = isSelected ? Color.white : new Color(0.14f, 0.14f, 0.14f, 1f);
                }
            }
        }

        private void UpdateUnitButtonsInteractable()
        {
            if (unitCarousel == null)
            {
                SetAllUnitButtonsInteractable(unitButtonsInteractable);
                return;
            }

            UpdateUnitButtonsInteractable(unitCarousel.CurrentCenterIndex);
        }

        private void UpdateUnitButtonsInteractable(int centerIndex)
        {
            for (int index = 0; index < unitButtonBindings.Count; index++)
            {
                UnitButtonBinding binding = unitButtonBindings[index];
                if (binding == null || binding.button == null) continue;

                bool active = unitButtonsInteractable && centerIndex >= 0 && index == centerIndex;
                binding.button.interactable = active;

                Image buttonImage = binding.button.GetComponent<Image>();
                if (buttonImage != null) buttonImage.raycastTarget = active;
            }
        }

        private void SetAllUnitButtonsInteractable(bool interactable)
        {
            for (int index = 0; index < unitButtonBindings.Count; index++)
            {
                UnitButtonBinding binding = unitButtonBindings[index];
                if (binding == null || binding.button == null) continue;

                binding.button.interactable = interactable;

                Image buttonImage = binding.button.GetComponent<Image>();
                if (buttonImage != null) buttonImage.raycastTarget = interactable;
            }
        }

        private void UpdateEntryPlayButtons()
        {
            if (entryButtonBindings.Count == 0) return;

            int centerIndex = entryCarousel != null ? entryCarousel.CurrentCenterIndex : (entryButtonBindings.Count > 0 ? 0 : -1);
            UpdateEntryPlayButtons(centerIndex);
        }

        private void UpdateEntryPlayButtons(int centerIndex)
        {
            if (entryButtonBindings.Count == 0) return;

            for (int index = 0; index < entryButtonBindings.Count; index++)
            {
                SetEntryPlayButtonState(entryButtonBindings[index], centerIndex >= 0 && index == centerIndex);
            }
        }

        private static void SetEntryPlayButtonState(EntryButtonBinding binding, bool enabled)
        {
            if (binding == null) return;

            if (binding.playObject != null) binding.playObject.SetActive(enabled);
            if (binding.playButton != null) binding.playButton.interactable = enabled;
        }

        private EntryButtonBinding CreateEntryButtonBinding(GameObject buttonObject, Button fallbackButton)
        {
            Transform playTransform = FindPlayButtonTransform(buttonObject != null ? buttonObject.transform : null);
            Button playButton = playTransform != null ? playTransform.GetComponent<Button>() : fallbackButton;

            return new EntryButtonBinding
            {
                root = buttonObject,
                playObject = playTransform != null ? playTransform.gameObject : (fallbackButton != null ? fallbackButton.gameObject : null),
                playButton = playButton
            };
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Button Clearing
        // ──────────────────────────────────────────────────────────────────────────

        private void ClearCategoryButtons()
        {
            for (int index = 0; index < generatedCategoryButtons.Count; index++)
            {
                if (generatedCategoryButtons[index] != null)
                    Destroy(generatedCategoryButtons[index]);
            }

            generatedCategoryButtons.Clear();
            categoryButtonBindings.Clear();
        }

        private void ClearUnitButtons()
        {
            for (int index = 0; index < generatedUnitButtons.Count; index++)
            {
                if (generatedUnitButtons[index] != null)
                    Destroy(generatedUnitButtons[index]);
            }

            generatedUnitButtons.Clear();
            unitButtonBindings.Clear();
            renderedUnits.Clear();
        }

        private void ClearEntryButtons()
        {
            for (int index = 0; index < generatedEntryButtons.Count; index++)
            {
                if (generatedEntryButtons[index] != null)
                    Destroy(generatedEntryButtons[index]);
            }

            generatedEntryButtons.Clear();
            entryButtonBindings.Clear();

            if (thumbnailLoader != null)
                thumbnailLoader.ClearTextures();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Carousel Resolver Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private ArcCarousel ResolveEntryCarousel()
        {
            if (interactiveButtonContainer == null) return null;

            ArcCarousel carousel = interactiveButtonContainer.GetComponent<ArcCarousel>();
            return carousel != null ? carousel : interactiveButtonContainer.GetComponentInParent<ArcCarousel>();
        }

        private ArcCarousel ResolveUnitCarousel()
        {
            if (unitButtonContainer == null) return null;

            ArcCarousel carousel = unitButtonContainer.GetComponent<ArcCarousel>();
            return carousel != null ? carousel : unitButtonContainer.GetComponentInParent<ArcCarousel>();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Static Utilities
        // ──────────────────────────────────────────────────────────────────────────

        private static void SetButtonLabel(GameObject buttonObject, string label)
        {
            if (buttonObject == null) return;

            TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                tmpText.text = label;
                return;
            }

            Text uiText = buttonObject.GetComponentInChildren<Text>(true);
            if (uiText != null) uiText.text = label;
        }

        private static RawImage FindThumbnailRawImage(GameObject buttonObject)
        {
            if (buttonObject == null) return null;

            Transform t = FindThumbnailTransform(buttonObject.transform);
            if (t != null)
            {
                RawImage ri = t.GetComponent<RawImage>();
                if (ri != null) return ri;
            }

            return buttonObject.GetComponentInChildren<RawImage>(true);
        }

        private static Image FindThumbnailImage(GameObject buttonObject, Button button)
        {
            if (buttonObject == null) return null;

            Transform t = FindThumbnailTransform(buttonObject.transform);
            if (t != null)
            {
                Image named = t.GetComponent<Image>();
                if (named != null) return named;
            }

            Image buttonImage = button != null ? button.GetComponent<Image>() : null;
            Image[] images = buttonObject.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++)
            {
                if (images[index] != null && images[index] != buttonImage)
                    return images[index];
            }

            return buttonImage;
        }

        private static Transform FindThumbnailTransform(Transform root)
        {
            return FindNamedChildTransform(root, "Image", "Thumbnail", "Thumb");
        }

        private static Transform FindPlayButtonTransform(Transform root)
        {
            return FindNamedChildTransform(root, "PlayButtonObj", "Play", "ButtonObj");
        }

        private static Transform FindNamedChildTransform(Transform root, params string[] names)
        {
            if (root == null) return null;
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                Transform found = FindChildRecursive(root, names[nameIndex]);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null) return nested;
            }
            return null;
        }

        private void SetStatus(string message)
        {
            if (!showBuiltInCatalogPanel || statusText == null) return;
            statusText.text = message;
        }
    }
}
