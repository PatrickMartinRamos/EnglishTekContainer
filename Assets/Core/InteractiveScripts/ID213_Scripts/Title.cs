using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID213
{
    public class Title : MonoBehaviour
    {
        public Button play;

        IEnumerator Start()
        {
            GameManager.Initialize();

            play.interactable = false;
            yield return new WaitForSeconds(2f);
            play.interactable = true;
        }

        public void Play()
        {
            StartCoroutine(Instructions());
        }

        IEnumerator Instructions()
        {
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("Instructions");

            //GameManager.Difficulty = "Practice";
            //GameManager.Initialize();
            //GameManager.GenerateItem();

        }
    }
}