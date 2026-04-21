using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID313
{
    public class Feedback : MonoBehaviour
    {
        public Text feedback;
        public SubmitScore ss;
        private int diff = 0;
        void Start()
        {
            if (GameManager.Difficulty == "Practice")
            {
                diff = 1;
            }
            else
            {
                diff = 2;
            }
            StartCoroutine(ss.PostScores(diff, GameManager.Score));
            feedback.text = GameManager.Feedback();
        }

        public void Play()
        {
            StartCoroutine(Instructions());
        }

        IEnumerator Instructions()
        {
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("Title");
            GameManager.Initialize();
        }
    }
}