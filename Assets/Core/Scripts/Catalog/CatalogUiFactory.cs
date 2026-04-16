using UnityEngine;
using UnityEngine.UI;

namespace EnglishTek.Core
{
    internal static class CatalogUiFactory
    {
        internal static Text CreateTextElement(string objectName, Transform parent, float fontSize)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // Default font included with Unity — replace with custom font if needed
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0f, 28f);

            return text;
        }

        internal static RawImage CreateThumbnailElement(Transform parent)
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

        internal static RectTransform CreateCategoryRow(Transform parent)
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

        internal static RectTransform CreateEntriesContainer(Transform parent)
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
    }
}
