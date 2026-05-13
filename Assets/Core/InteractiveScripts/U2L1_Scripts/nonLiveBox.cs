using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace ScienceTek.Grade3.U2L1
{
    public class nonLiveBox : MonoBehaviour,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Correct Feedback")]
        [SerializeField] private GameObject check;
        [SerializeField] private AudioSource correctAudio;

        [Header("Wrong Feedback")]
        [SerializeField] private GameObject x;

        [SerializeField] private float showDuration = 2f;

        private Animator animator;

        private void Awake()
        {
            if (check != null) check.SetActive(false);
            if (x != null) x.SetActive(false);

            animator = GetComponent<Animator>();
        }

        // 🔹 NEW: trigger while dragging over box
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                animator?.SetBool("isColliding", true);
            }
        }

        // 🔹 NEW: reset when leaving box
        public void OnPointerExit(PointerEventData eventData)
        {
            animator?.SetBool("isColliding", false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            animator?.SetBool("isColliding", true);

            GameObject item = eventData.pointerDrag;
            UIDragItem dragItem = item.GetComponent<UIDragItem>();

            if (item.CompareTag("nonLiving"))
            {
                ShowCheck();
                dragItem?.MarkDroppedOnValidBox();
                dragItem?.HideItem();
            }
            else
            {
                ShowX();
            }
        }

        private void ShowCheck()
        {
            if (correctAudio != null)
                correctAudio.Play();

            StartCoroutine(ShowForSeconds(check));
        }

        private void ShowX()
        {
            StartCoroutine(ShowForSeconds(x));
        }

        private IEnumerator ShowForSeconds(GameObject obj)
        {
            if (obj == null) yield break;

            obj.SetActive(true);
            yield return new WaitForSeconds(showDuration);
            obj.SetActive(false);

            animator?.SetBool("isColliding", false);
        }
    }

}