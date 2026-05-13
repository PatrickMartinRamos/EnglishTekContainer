using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class pondComplete : MonoBehaviour
    {
        [SerializeField] private GameObject[] items;

        public GameObject complete;

        public ProgressChecker progressChecker;

        void Update()
        {
            if (items.All(item => !item.activeSelf))
            {
                progressChecker.pondComplete = true;
                Debug.Log(progressChecker.pondComplete);
                complete.SetActive(true);
            }
        }
    }

}