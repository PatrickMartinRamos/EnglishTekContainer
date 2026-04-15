using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID106
{
    public class Feedback : MonoBehaviour
    {
        public Text feedback;
        public SubmitScore ss;
        void Start()
        {
            feedback.text = GameManager.Feedback();
            var diff = 0;
            if (GameManager.Difficulty == "Practice")
            {
                diff = 1;
            }
            else if (GameManager.Difficulty == "Quiz")
            {
                diff = 2;
            }
            StartCoroutine(ss.PostScores(diff, GameManager.Score));
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