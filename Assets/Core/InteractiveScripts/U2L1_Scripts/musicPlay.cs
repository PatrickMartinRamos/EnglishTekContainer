using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class musicPlay : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource;

        void Start()
        {
            musicSource.Play();
        }
    }

}