using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID213
{ 
    public class Difficulty : MonoBehaviour
    {
        public void StartGame(string difficulty)
        {
            GameManager.Difficulty = difficulty;
            GameManager.GenerateItem();
            StartCoroutine(Game());
        }

        IEnumerator Game()
        {            
            yield return new WaitForSeconds(0.5f);
            SceneManager.LoadScene("Game");
        }
    }
}
