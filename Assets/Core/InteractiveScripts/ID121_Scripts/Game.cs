using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID121
{
    public class Game : MonoBehaviour
    {
        public Text question;
        public Text score;
        public Text item;
        public Text instructions;
        public Text correctText;

        public GameObject correct;
        public GameObject wrong;

        public Transform stoneContainer;
        public Transform canContainer;
        public GameObject stone;
        public GameObject can;

        public GameObject questionPanel;
        Slipper slipper;

        void Start()
        {
            slipper = FindObjectOfType<Slipper>();
            instructions.text = GameManager.Instructions;
            NextItem();
        }

        void NextItem()
        {
            GameManager.NextItem();
            question.text = GameManager.Question;
            question.text = question.text.Replace("(", "<b>");
            question.text = question.text.Replace(")", "</b>");
            item.text = GameManager.Item.ToString("00");
            score.text = GameManager.Score.ToString("00");
            correctText.text = GameManager.Correct;

            questionPanel.SetActive(true);
            slipper.Initialize();
            FillStone();
            FillCan();
        }

        void FillStone()
        {
            for (int i = 0; i < stoneContainer.childCount; i++) { Destroy(stoneContainer.GetChild(i).gameObject); }

            var xPos = new Vector2(-350f, 350f);
            var yPos = new Vector2(-100f, 180f);            

            int totalStone = 0;
            switch (GameManager.Difficulty)
            {
                case "Practice": totalStone = 2; break;
                case "Workout": totalStone = 3; break;
                case "Quiz": totalStone = 4; break;
            }

            int stoneCount = Random.Range(totalStone - 2, totalStone);

            for (int i = 0; i < stoneCount; i++)
            {
                var _stone = Instantiate(stone);
                var x = Random.Range(xPos.x, xPos.y);
                var y = Random.Range(yPos.x, yPos.y);

                _stone.transform.SetParent(stoneContainer);
                _stone.transform.localScale = Vector3.one;
                _stone.transform.localPosition = new Vector3(x, y, 0f);
                _stone.name = "Stone";
            }
        }

        void FillCan()
        {
            string[] _choices = GameManager.Choices.Split(',');
            for (int i = 0; i < canContainer.childCount; i++) { Destroy(canContainer.GetChild(i).gameObject); }

            var xPos = new Vector2(-240f, 240f);
            var yPos = new Vector2(-110f, 150f);

            for (int i = 0; i < _choices.Length; i++)
            {
                var _can = Instantiate(can);
                var x = Random.Range(xPos.x, xPos.y);
                var y = Random.Range(yPos.x, yPos.y);

                _can.transform.SetParent(canContainer);
                _can.transform.localScale = Vector3.one;
                _can.transform.localPosition = new Vector3(x, y, 0f);
                _can.name = "Can";
                _can.GetComponentInChildren<Text>().text = _choices[i];
            }
        }

        public void CheckAnswer(string answer)
        {           
            bool isCorrect = GameManager.CheckAnswer(answer);
            if (isCorrect)
            {
                GameManager.Score++;
                score.text = GameManager.Score.ToString("00");

                correct.SetActive(true);
                correct.GetComponent<AudioSource>().Play();                
            }
            else
            {
                wrong.SetActive(true);
                wrong.GetComponent<AudioSource>().Play();
            }

            StartCoroutine(AnswerFeedback(isCorrect));
        }

        IEnumerator AnswerFeedback(bool isCorrect)
        {            
            yield return new WaitForSeconds(2f); // orig | yield return new WaitForSeconds(7f);

            if (isCorrect) correct.SetActive(false);
            else wrong.SetActive(false);

            if (GameManager.Item >= GameManager.TotalItem)
                SceneManager.LoadScene("Feedback");
            else
                NextItem();        
        }

        public void Sounds(Button sound)
        {
            var soundText = sound.GetComponentInChildren<Text>();
            var listener = FindObjectOfType<AudioListener>();
            if (soundText.text == "SOUND ON")
            {
                soundText.text = "SOUND OFF";
                listener.enabled = false;
            }
            else
            {
                soundText.text = "SOUND ON";
                listener.enabled = true;
            }
            
        }

        public void Instructions()
        {
            
        }
    }
}
