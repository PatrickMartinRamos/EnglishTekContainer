namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;

    public class doneBTN : MonoBehaviour
    {
        public gameplay gameplayScript;
        Animator animator;
        //public GameObject b1,b2,title,
        public GameObject gd,title;
        void Awake()
        {
            animator = GetComponent<Animator>();
            animator.SetBool("isHolding", false);
        }

        void OnMouseDown()
        {
            animator.SetBool("isHolding", true);
        }

        void OnMouseUp()
        {
            animator.SetBool("isHolding", false);
            AudioListener.volume = 1f;
            gd.SetActive(false);
            title.SetActive(true);
            gameplayScript.recolor();
        }
    }

}