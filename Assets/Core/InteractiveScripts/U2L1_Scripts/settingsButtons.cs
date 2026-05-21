using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class settingsButtons : MonoBehaviour
    {
        [SerializeField] private Animator instructionsAnimator; // Animator for Instructions tab
        [SerializeField] private GameObject instructionsTab;    // Instructions panel

        [SerializeField] private Animator audioAnimator;        // Animator for Audio tab
        [SerializeField] private GameObject audioTab;          // Audio panel


        void Update()
        {
            if (instructionsTab.activeSelf)
            {
                instructionsAnimator.SetBool("isClick", true);
            }

            else
            {
                instructionsAnimator.SetBool("isClick", false);
            }

            if (audioTab.activeSelf)
            {
                audioAnimator.SetBool("isClicked", true);
            }

            else
            {audioAnimator.SetBool("isClicked", false);
            }
        }

        public void ShowInstructionsTab()
        {
            instructionsTab.SetActive(true);
            audioTab.SetActive(false);
        }

        public void ShowAudioTab()
        {
            audioTab.SetActive(true);
            instructionsTab.SetActive(false);
        }
    }

}