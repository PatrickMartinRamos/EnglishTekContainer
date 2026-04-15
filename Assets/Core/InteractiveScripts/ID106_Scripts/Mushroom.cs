using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishTek.Grade1.ID106
{
    public class Mushroom : MonoBehaviour
    {
        Game game;
       
        void Start() {
            game = FindObjectOfType<Game>();
        }

        public void Click()
        {
            game.CheckAnswer(GetComponentInChildren<Text>().text);
        }
    }
}