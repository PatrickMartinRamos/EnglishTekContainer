namespace ScienceTek.Grade3.U1L5
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO.Compression;
    using UnityEngine;

    public class hideTab : MonoBehaviour
    {
        public GameObject iron, silver, gold, copper;

        void OnMouseUp()
        {
            if (iron.activeSelf || silver.activeSelf || gold.activeSelf || copper.activeSelf)
            {
                hideTabs();
            }
        }

        public void hideTabs()
        {
            iron.SetActive(false);
            silver.SetActive(false);
            gold.SetActive(false);
            copper.SetActive(false);
        }
    }

}