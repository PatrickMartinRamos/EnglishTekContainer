using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade2.ID232
{
    public class Title : MonoBehaviour
    {
        public Button play;
		public GameObject title_idle;
		public GameObject title_entrance;

        IEnumerator Start()
        {
            GameManager.Initialize();

            play.interactable = false;
            yield return new WaitForSeconds(0f);
            play.interactable = true;
        }

        public void Play()
        {
			title_idle.SetActive (false);
			title_entrance.SetActive (true);
            StartCoroutine(Instructions());
        }

        IEnumerator Instructions()
        {
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene("Instructions");

            //GameManager.Difficulty = "Practice";
            //GameManager.Initialize();
            //GameManager.GenerateItem();
        }
    }
}