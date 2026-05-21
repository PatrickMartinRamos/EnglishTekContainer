using UnityEngine;
using UnityEngine.EventSystems;

namespace ScienceTek.Grade3.U1L5
{
    public class backButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public GameObject settings;
        Animator anim;

        void Awake() => anim = GetComponent<Animator>();

        public void OnPointerDown(PointerEventData eventData)
        {
            anim.SetBool("isHolding", true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            anim.SetBool("isHolding", false);
            if (settings != null) settings.SetActive(false);
        }
    }
}