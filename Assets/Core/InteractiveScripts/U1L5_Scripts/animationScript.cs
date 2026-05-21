namespace ScienceTek.Grade3.U1L5
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.EventSystems;
    using UnityEngine;

    public class animationScript : MonoBehaviour,
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