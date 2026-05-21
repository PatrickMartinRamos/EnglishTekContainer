using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

namespace ScienceTek.Grade3.U2L1
{
    public class ProgressChecker : MonoBehaviour
    {
        public bool pondComplete = false;
        public bool communityComplete = false;

        public GameObject pondCheck, communityCheck, bothCheck;
        public GameObject pond, community;

        void Update()
        {
            if (pondComplete && !communityComplete)
            {
                disablePond();
                pondCheck.SetActive(true);
            }

            if (communityComplete && !pondComplete)
            {
                disableCommunity();
                communityCheck.SetActive(true);
            }

            if (pondComplete && communityComplete)
            {
                disablePond();
                disableCommunity();
                pondCheck.SetActive(false);
                communityCheck.SetActive(false);
                bothCheck.SetActive(true);
            }
        }

        public void disablePond()
        {
            Button btn = pond.GetComponent<Button>();
            btn.interactable = false;
        }

        public void disableCommunity()
        {
            Button btn = community.GetComponent<Button>();
            btn.interactable = false;
        }

    }

}