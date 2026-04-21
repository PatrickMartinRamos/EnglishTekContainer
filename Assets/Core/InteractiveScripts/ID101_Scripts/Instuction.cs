using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace FilipinoTek.Grade2.ID101
{
    public class Instuction : MonoBehaviour
    {

        [SerializeField] List<AudioClip> instructionSound;
        [SerializeField] AudioSource audioSource;

        // Start is called before the first frame update
        void Start()
        {
            SFX_Instruction();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SFX_Instruction()
        {
            if (GameManager.Difficulty == "Level 1")
            {
                //audioSource.clip = instructionSound[0];
               // audioSource.Play();
            }
            else if (GameManager.Difficulty == "Level 2")
            {
                //audioSource.clip = instructionSound[1];
                //audioSource.Play();
            }

            else if (GameManager.Difficulty == "Level 3")
            {
                //audioSource.clip = instructionSound[2];
                //audioSource.Play();
            }

        }
    }


}

