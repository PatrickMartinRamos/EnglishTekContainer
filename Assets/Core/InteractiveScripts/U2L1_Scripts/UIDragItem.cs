using UnityEngine;
using UnityEngine.EventSystems;

namespace ScienceTek.Grade3.U2L1
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Animator))]
    public class UIDragItem : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Animator animator;

        private Vector2 startPosition;
        private bool droppedOnValidBox;

        public bool isDragging { get; private set; }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
            animator = GetComponent<Animator>();

            startPosition = rectTransform.anchoredPosition;
        }

        private void SetDragging(bool value)
        {
            isDragging = value;
            animator.SetBool("isDragging", value);
        }

        public void MarkDroppedOnValidBox()
        {
            droppedOnValidBox = true;
        }

        public void ResetPosition()
        {
            rectTransform.anchoredPosition = startPosition;
        }

        public void HideItem()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isDragging)
                SetDragging(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!eventData.dragging)
                SetDragging(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetDragging(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            droppedOnValidBox = false; // reset flag
            SetDragging(true);
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SetDragging(false);
            canvasGroup.blocksRaycasts = true;

            // If no valid box accepted this item → return it
            if (!droppedOnValidBox)
            {
                ResetPosition();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetDragging(false);
        }
    }

}