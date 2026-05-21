namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;

    public class BGM : MonoBehaviour
    {
        private static BGM instance;
        public AudioSource soundeffect;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            AudioSource audio = GetComponent<AudioSource>();
            if (!audio.isPlaying)
                audio.Play();
        }

        public void soundEffect()
        {
            soundeffect.Play();
        }
    }

}