using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class BGM : MonoBehaviour
    {
        private static BGM instance;

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
    }

}