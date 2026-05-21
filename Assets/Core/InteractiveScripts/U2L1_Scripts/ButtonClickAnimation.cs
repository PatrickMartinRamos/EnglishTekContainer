using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class ButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {   
        private Animator animator;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            animator.SetBool("isHolding", true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            animator.SetBool("isHolding", false);
        }
    }

}