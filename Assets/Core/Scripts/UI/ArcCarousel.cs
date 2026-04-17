using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EnglishTek.Core
{
    /// <summary>
    /// Revolver-style arc carousel. Items are arranged in a fixed arc;
    /// swiping rotates them along the arc and snaps to the nearest item.
    ///
    /// Setup — replace the ScrollRect + Content approach with:
    ///   CarouselRoot  [ArcCarousel + Image (color A=0, for raycasts)]
    ///     ├── UnitButton_1
    ///     ├── UnitButton_2
    ///     └── ...
    ///
    /// The CarouselRoot should be a plain RectTransform sized to your visible area.
    /// Do not put this inside a ScrollRect.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]   // transparent image needed for drag raycasts
    public class ArcCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Item Size")]
        [SerializeField] private float itemWidth = 120f;
        [SerializeField] private float itemHeight = 60f;
        [SerializeField] private float spacing = 16f;

        [Header("Arc Shape")]
        [Tooltip("How many pixels the center item rises above the edge items.")]
        [SerializeField] private float arcHeight = 50f;

        [Tooltip("How many items to each side of center are considered 'at the edge' of the arc. " +
                 "Items beyond this still exist but are at base height.")]
        [SerializeField] private int visibleRadius = 3;

        [Tooltip("Optionally scale down items further from center (1 = no scaling).")]
        [SerializeField] private float edgeScale = 0.75f;

        [Header("Snap")]
        [Tooltip("Speed at which the carousel snaps to the nearest item after drag.")]
        [SerializeField] private float snapSpeed = 10f;

        [Tooltip("Pixels of drag needed to advance one item. Auto-computed from item size if zero.")]
        [SerializeField] private float dragStep = 0f;

        // ------------------------------------------------------------------ runtime state

        private float currentOffset = 0f;   // fractional — which index is at center
        private float targetOffset = 0f;
        private bool isDragging = false;
        private float dragAccum = 0f;       // accumulated px since drag began
        private float offsetAtDragStart = 0f;

        /// <summary>Fired when the centered item index changes (after snapping).</summary>
        public event Action<int> OnCenterIndexChanged;

        private readonly List<RectTransform> items = new List<RectTransform>();
        private RectTransform rectTransform;
        private int lastCenterIndex = -1;

        public int CurrentCenterIndex
        {
            get
            {
                if (items.Count == 0)
                {
                    return -1;
                }

                return Mathf.Clamp(Mathf.RoundToInt(currentOffset), 0, items.Count - 1);
            }
        }

        // ------------------------------------------------------------------ Unity messages

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // Make the backing Image transparent so it catches drag events
            // without drawing anything visible.
            Image img = GetComponent<Image>();
            if (img != null && img.color.a > 0.01f)
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
            }
        }

        private void OnEnable()
        {
            RefreshItems();
            PositionAll();
            NotifyCenterIndexChangedIfNeeded();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshItems();
            PositionAll();
            NotifyCenterIndexChangedIfNeeded();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (isDragging)
            {
                return;
            }

            float prev = currentOffset;
            currentOffset = Mathf.Lerp(currentOffset, targetOffset, Time.deltaTime * snapSpeed);

            if (Mathf.Abs(currentOffset - prev) > 0.0005f)
            {
                PositionAll();
            }

            NotifyCenterIndexChangedIfNeeded();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            RefreshItems();
            PositionAll();
        }
#endif

        // ------------------------------------------------------------------ drag

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            dragAccum = 0f;
            offsetAtDragStart = currentOffset;
        }

        public void OnDrag(PointerEventData eventData)
        {
            dragAccum += eventData.delta.x;
            float step = EffectiveDragStep();
            // Dragging left (negative x) advances the carousel to the right (higher index).
            currentOffset = offsetAtDragStart - dragAccum / step;
            currentOffset = Mathf.Clamp(currentOffset, 0f, Mathf.Max(0f, items.Count - 1));
            PositionAll();
            NotifyCenterIndexChangedIfNeeded();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            targetOffset = Mathf.Round(currentOffset);
            targetOffset = Mathf.Clamp(targetOffset, 0f, Mathf.Max(0f, items.Count - 1));
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Navigate to a specific item index with snapping animation.</summary>
        public void GoToIndex(int index)
        {
            targetOffset = Mathf.Clamp(index, 0, Mathf.Max(0, items.Count - 1));
        }

        /// <summary>Force an immediate rebuild of item positions.</summary>
        public void Rebuild()
        {
            RefreshItems();
            PositionAll();
        }

        // ------------------------------------------------------------------ positioning

        private void PositionAll()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (rectTransform == null || items.Count == 0)
            {
                return;
            }

            float cx = rectTransform.rect.width * 0.5f;
            float step = itemWidth + spacing;

            for (int i = 0; i < items.Count; i++)
            {
                RectTransform rt = items[i];
                if (rt == null)
                {
                    continue;
                }

                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(itemWidth, itemHeight);

                // Distance of this item from the centered index (can be fractional).
                float rel = i - currentOffset;

                // X: centered in the container, spread by step.
                float xPos = cx + rel * step;

                // Y: cosine arc — center item is highest, edge items are at baseline.
                // Clamp so items beyond visibleRadius don't go below baseline.
                float cosArg = Mathf.Clamp(rel / Mathf.Max(1, visibleRadius), -1f, 1f);
                float yOffset = arcHeight * Mathf.Cos(cosArg * Mathf.PI * 0.5f);
                yOffset = Mathf.Max(0f, yOffset);

                float yPos = itemHeight * 0.5f + yOffset;

                rt.anchoredPosition = new Vector2(xPos, yPos);

                // Scale: lerp between edgeScale and 1 based on distance from center.
                float t = Mathf.Clamp01(1f - Mathf.Abs(rel) / Mathf.Max(1, visibleRadius));
                float s = Mathf.Lerp(edgeScale, 1f, t);
                rt.localScale = new Vector3(s, s, 1f);
            }
        }

        // ------------------------------------------------------------------ helpers

        private void RefreshItems()
        {
            items.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                RectTransform rt = child as RectTransform;
                if (rt != null)
                {
                    items.Add(rt);
                }
            }

            float maxOffset = Mathf.Max(0f, items.Count - 1);
            currentOffset = Mathf.Clamp(currentOffset, 0f, maxOffset);
            targetOffset = Mathf.Clamp(targetOffset, 0f, maxOffset);
        }

        private float EffectiveDragStep()
        {
            return dragStep > 0f ? dragStep : itemWidth + spacing;
        }

        private void NotifyCenterIndexChangedIfNeeded()
        {
            int centerIndex = CurrentCenterIndex;
            if (centerIndex == lastCenterIndex)
            {
                return;
            }

            lastCenterIndex = centerIndex;
            OnCenterIndexChanged?.Invoke(centerIndex);
        }
    }
}
