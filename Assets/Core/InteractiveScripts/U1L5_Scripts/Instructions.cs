namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;

    public class Instructions: MonoBehaviour
    {
        public GameObject inst, sound; 
        Animator anim;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        void OnMouseDown()
        {
            anim.SetBool("isHolding", true); 
        }

        void OnMouseUp()
        {
            anim.SetBool("isHolding", false);

            inst.SetActive(true);
            sound.SetActive(false);
        }
    }

}