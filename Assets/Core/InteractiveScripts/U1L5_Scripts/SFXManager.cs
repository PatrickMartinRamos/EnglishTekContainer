namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;

    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance;

        [Range(0f, 1f)] public float musicVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

}