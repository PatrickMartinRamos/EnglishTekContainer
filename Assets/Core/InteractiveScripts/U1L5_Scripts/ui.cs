namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class ui : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        Animator anim;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            anim.SetBool("isHovering", true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            anim.SetBool("isHovering", false);
        }
    }

}