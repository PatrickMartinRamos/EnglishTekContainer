namespace ScienceTek.Grade3.U1L5
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class gameplay : MonoBehaviour
    {
        public GameObject bg1, bg2, title, settings, items, guide;
        public GameObject copper, gold, iron, silver, next, click;
        public Image nailSR, spoonSR, coinSR, wireSR;
        public GameObject gameScene, guideQuestions,done;

        void Update()
        {
            if (settings.activeSelf)
            {
                items.SetActive(false);
                guide.SetActive(false);
            }

            else
            {
                items.SetActive(true);
                guide.SetActive(true);
            }

            if (next.activeSelf)
            {
                guide.SetActive(false);
            }
            
            if (settings.activeSelf)
            {
                next.GetComponent<Button>().interactable = false;
                done.SetActive(false);
            }
            else
            {
                next.GetComponent<Button>().interactable = true;
                done.SetActive(true);
            }
        }

        public void showGuideQuestions()
        {
            bg2.SetActive(false);
            bg1.SetActive(true);
            guideQuestions.SetActive(true);
        }

        public void hideGame()
        {
            gameScene.SetActive(false);
        }

        public void showGame()
        {
            gameScene.SetActive(true);
        }

        public void allClicked()
        {
            if (nailSR.color == Color.gray &&
                spoonSR.color == Color.gray &&
                coinSR.color == Color.gray &&
                wireSR.color == Color.gray)
            {
                click.SetActive(false);
                next.SetActive(true);
            }
        }

        public void recolor()
        {
            nailSR.color = Color.white;
            spoonSR.color = Color.white;
            coinSR.color = Color.white;
            wireSR.color = Color.white;
        }

        public void nailColor()
        {
            nailSR.color = Color.gray;
            allClicked();
        }

        public void spoonColor()
        {
            spoonSR.color = Color.gray;
            allClicked();
        }

        public void coinColor()
        {
            coinSR.color = Color.gray;
            allClicked();
        }

        public void wireColor()
        {
            wireSR.color = Color.gray;
            allClicked();
        }

        public void hideMain()
        {
            bg1.SetActive(false);
            title.SetActive(false);

            bg2.SetActive(true);
        }

        public void showIron()
        {
            iron.SetActive(true);
        }

        public void showGold()
        {
            gold.SetActive(true);
        }

        public void showCopper()
        {
            copper.SetActive(true);
        }

        public void showSilver()
        {
            silver.SetActive(true);
        }   
    }

}