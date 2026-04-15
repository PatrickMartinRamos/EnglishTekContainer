using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID213
{
    public class Game : MonoBehaviour
    {
        public GameObject starfish;
        public GameObject shell;
        public Character character;
        public Text question;
        public Text score;
        public Text item;
        public Text instructions;
        public Vector2 minPos;
        public Vector2 maxPos;
        public Animator correct;
        public Animator wrong;

        // Text components for displaying answers on starfish and shell
        public Text starfishAnswerText;
        public Text shellAnswerText;

        // Store which answer is correct
        private string starfishAnswer;
        private string shellAnswer;

        void Start()
        {
            instructions.text = GameManager.Instructions;
            NextItem();
        }

        void NextItem()
        {
            character.Initialize();

        // Position starfish
        POSITION_STARFISH:
            var randomPos = new Vector3(Random.Range(minPos.x, maxPos.x), Random.Range(minPos.y, maxPos.y), -0.1f);
            starfish.transform.localPosition = randomPos;
            if (starfish.transform.localPosition == character.transform.localPosition)
                goto POSITION_STARFISH;

            // Position shell
            POSITION_SHELL:
            randomPos = new Vector3(Random.Range(minPos.x, maxPos.x), Random.Range(minPos.y, maxPos.y), -0.1f);
            shell.transform.localPosition = randomPos;
            if (shell.transform.localPosition == character.transform.localPosition || shell.transform.localPosition == starfish.transform.localPosition)
                goto POSITION_SHELL;

            shell.SetActive(true);
            starfish.SetActive(true);

            GameManager.NextItem();

            // Display question
            string _question = GameManager.Question.Replace("(", "<color=red>");
            _question = _question.Replace(")", "</color>");
            question.text = _question;
            item.text = GameManager.Item.ToString("00");
            score.text = GameManager.Score.ToString("00");

            // Set answers based on difficulty
            if (GameManager.Difficulty == "Practice")
            {
                // For practice mode, starfish is always "Yes", shell is always "No"
                starfishAnswer = "Yes";
                shellAnswer = "No";

                // Display "Yes" and "No" in Practice mode
                if (starfishAnswerText != null)
                    starfishAnswerText.text = "Yes";
                if (shellAnswerText != null)
                    shellAnswerText.text = "No";
            }
            else if (GameManager.Difficulty == "Quiz" || GameManager.Difficulty == "Workout")
            {
                RandomizeAnswersOnObjects();
            }
        }

        void RandomizeAnswersOnObjects()
        {
            // Random number: 0 or 1
            int random = Random.Range(0, 2);

            if (random == 0)
            {
                // Starfish gets correct, Shell gets wrong
                starfishAnswer = GameManager.Correct;
                shellAnswer = GameManager.Wrong;
            }
            else
            {
                // Starfish gets wrong, Shell gets correct
                starfishAnswer = GameManager.Wrong;
                shellAnswer = GameManager.Correct;
            }

            // Display the answers on the objects
            if (starfishAnswerText != null)
                starfishAnswerText.text = starfishAnswer;

            if (shellAnswerText != null)
                shellAnswerText.text = shellAnswer;
        }

        public void CheckAnswer(string answer)
        {
            bool isCorrect = GameManager.CheckAnswer(answer);
            if (isCorrect)
            {
                GameManager.Score++;
                score.text = GameManager.Score.ToString("00");
                correct.SetTrigger("play");
                correct.GetComponent<AudioSource>().Play();
            }
            else
            {
                wrong.SetTrigger("play");
                wrong.GetComponent<AudioSource>().Play();
            }
            StartCoroutine(AnswerFeedback(isCorrect));
        }

        IEnumerator AnswerFeedback(bool isCorrect)
        {
            yield return new WaitForSeconds(2f);
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
            FindObjectOfType<Character>().Move(false);
        }

        // Public method to get the starfish answer
        public string GetStarfishAnswer()
        {
            return starfishAnswer;
        }

        // Public method to get the shell answer
        public string GetShellAnswer()
        {
            return shellAnswer;
        }
    }
}