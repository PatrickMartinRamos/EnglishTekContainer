namespace ScienceTek.Grade3.U1L5
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class settingsScript : MonoBehaviour
    {
        public GameObject instructions, sound;
        public GameObject instructionsBtn, soundBtn;

        public GameObject settings;

        void Update()
        {   
            if (instructions.activeSelf)
            {
                sound.SetActive(false);
                instructionsBtn.SetActive(false);
                soundBtn.SetActive(true);
            }

            if (sound.activeSelf)
            {
                instructions.SetActive(false);
                soundBtn.SetActive(false);
                instructionsBtn.SetActive(true);
            }
        }

        public void openSettings()
        {
            settings.SetActive(true);
        }
    }

}