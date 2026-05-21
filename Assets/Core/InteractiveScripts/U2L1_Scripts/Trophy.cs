using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class Trophy : MonoBehaviour
    {
        public void AddScore()
        {
            FindObjectOfType<Game>().AddScore();
        }
    }
}