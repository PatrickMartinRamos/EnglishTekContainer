using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID213
{
    public class Instructions : MonoBehaviour
    {
        public Text instructions;

        void Start()
        {
            instructions.text = GameManager.Instructions;
        }

        public void StartGame()
        {
            StartCoroutine(Game());
        }

        IEnumerator Game()
        {
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("Difficulty");
        }
    }
}