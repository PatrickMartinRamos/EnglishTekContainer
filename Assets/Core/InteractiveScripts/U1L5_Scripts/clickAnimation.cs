namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;

    public class clickAnimation: MonoBehaviour
    {
        public GameObject inst, sound; 
        Animator anim;

        void Awake()
        {
            anim = GetComponent<Animator>();
        }

        void OnMouseDown()
        {
            anim.SetBool("isClicking", true); 
        }

        void OnMouseUp()
        {
            anim.SetBool("isClicking", false);

            sound.SetActive(true);
            inst.SetActive(false);
        }
    }

}