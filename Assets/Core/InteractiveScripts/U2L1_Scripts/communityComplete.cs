using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class communityComplete : MonoBehaviour
    {
        [SerializeField] private GameObject[] items;

        public GameObject complete;

        public ProgressChecker progressChecker;

        void Update()
        {
            if (items.All(item => !item.activeSelf))
            {
                progressChecker.communityComplete = true;
                Debug.Log(progressChecker.communityComplete);
                complete.SetActive(true);
            }
        }
    }

}