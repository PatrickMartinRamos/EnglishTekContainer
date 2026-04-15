using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID106
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
            StartCoroutine(Difficulty());
        }

        IEnumerator Difficulty()
        {
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("Difficulty");
        }
    }
}