using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScienceTek.Grade3.U2L1
{
    public class gameplay : MonoBehaviour
    {
        public GameObject main, selection, pond, community,GD, settings;

        public void Done()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void hideSettings()
        {
            settings.SetActive(false);
        }

        public void showSettings()
        {
            settings.SetActive(true);
        }

        public void showGD()
        {
            selection.SetActive(false);
            GD.SetActive(true);
        }

        public void showSelectionNoDelay()
        {
            pond.SetActive(false);
            community.SetActive(false);
            selection.SetActive(true);
        }

        public void showPond()
        {
            pond.SetActive(true);
            selection.SetActive(false);
        }

        
        public void showCommunity()
        {
            community.SetActive(true);
            selection.SetActive(false);
        }

        public void showMainDelayed()
        {
            Delay(0f, showMain);
        }

        public void showSelectionDelayed()
        {
            Delay(0.3f, showSelection);
        }

        public void showMain()
        {
            main.SetActive(true);
            selection.SetActive(false);
        }

        public void showSelection()
        {
            main.SetActive(false);
            selection.SetActive(true);
        }

        public void Delay(float delay, Action action)
        {
            StartCoroutine(DelayCoroutine(delay, action));
        }

        private IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

    }

}