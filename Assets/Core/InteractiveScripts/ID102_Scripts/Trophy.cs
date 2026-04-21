using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FilipinoTek.Grade2.ID102
{
    public class Trophy : MonoBehaviour
    {
        public void AddScore()
        {
            FindObjectOfType<Game>().AddScore();
        }
    }
}