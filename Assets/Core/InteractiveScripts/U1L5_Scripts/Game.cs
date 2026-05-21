using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ScienceTek.Grade3.U1L5
{
    public class Game : MonoBehaviour
    {
        [SerializeField] Text instructions;
        [SerializeField] Text scoreText, feedbackScoreText;
        [SerializeField] Text answerText;
        [SerializeField] Text feedbackText;
        [SerializeField] Animator trophy;
        [SerializeField] Transform itemsParent;
        [SerializeField] GameObject itemPrefab;
        [SerializeField] GameObject feedback;
        [SerializeField] Animator starAnimator;
        [SerializeField] Settings settings;

        [SerializeField] Text questionText;

        public Transform[] choices;

        [SerializeField] GameObject correctFeedback, wrongFeedback;
        private List<int> answers;

        private int diff = 0;
        private bool isClickable, isScalerNegative, isScalerPositive;

        [SerializeField] List<Button> choicesButton;

        [SerializeField] List<AudioClip> sound_effect;
        [SerializeField] AudioSource sound_effect_audiosource;

        [SerializeField] GameObject choicesbutton2;
        [SerializeField] GameObject choicesbutton3;
        [SerializeField] GameObject choicesbutton4;

        [SerializeField] List<Button> choicesSet2;
        [SerializeField] List<Button> choicesSet3;
        [SerializeField] List<Button> choicesSet4;

        [SerializeField] GameObject QuestionAndChoices;

        private float ValStart;

        private void Start()
        {

        }

        private void Update()
        {
            float valCur = 0.05f;

            if (isScalerNegative)
            {
                ValStart -= valCur;
                QuestionAndChoices.transform.localScale = new Vector3(ValStart, 1, 1);

                if (ValStart <= 0)
                    isScalerNegative = false;
            }

            else if (isScalerPositive)
            {
                ValStart += valCur;
                QuestionAndChoices.transform.localScale = new Vector3(ValStart, 1, 1);

                if (ValStart >= 1)
                    isScalerPositive = false;
            }
        }

        public void SoundEffect0()
        {
            sound_effect_audiosource.clip = sound_effect[0];
            sound_effect_audiosource.Play();
        }

        public void SoundEffect1()
        {
            sound_effect_audiosource.clip = sound_effect[1];
            sound_effect_audiosource.Play();
        }


        public void AddScore() /* add score if correct and animate throphy */
        {
            GameManager.Score++;
            scoreText.text = GameManager.Score.ToString();
            
        }

        public void NextItem() /* Main Updater per item */
        {
            GameManager.NextItem();
            
            questionText.text = GameManager.Question;
            instructions.text = GameManager.Instructions;

            string[] _choices = GameManager.Choices.Split('|');

            //group choices button based on number of choices

            if (_choices.Length == 2)
            {
                choicesbutton2.SetActive(true);
                choicesbutton3.SetActive(false);
                choicesbutton4.SetActive(false);


                for (int i = 0; i < _choices.Length; i++)
                {
                    choicesSet2[i].GetComponentInChildren<Text>().text = _choices[i];
                    choicesSet2[i].gameObject.SetActive(true);
                }
            }

            if (_choices.Length == 3)
            {
                choicesbutton2.SetActive(false);
                choicesbutton3.SetActive(true);
                choicesbutton4.SetActive(false);

                for (int i = 0; i < _choices.Length; i++)
                {
                    choicesSet3[i].GetComponentInChildren<Text>().text = _choices[i];
                    choicesSet3[i].gameObject.SetActive(true);
                }

            }

            if (_choices.Length == 4)
            {
                choicesbutton2.SetActive(false);
                choicesbutton3.SetActive(false);
                choicesbutton4.SetActive(true);

                for (int i = 0; i < _choices.Length; i++)
                {
                    choicesSet4[i].GetComponentInChildren<Text>().text = _choices[i];
                    choicesSet4[i].gameObject.SetActive(true);
                }
            }

            answers = new List<int>();
            foreach (var item in itemsParent.GetComponentsInChildren<Toggle>())
            {
                if (!item.isOn)
                {
                    item.isOn = true;
                    break;
                }
            }

            
       
            //ValStart = 1;
            isScalerNegative = false;
            isScalerPositive = true;


            isClickable = true;
            correctFeedback.SetActive(false);
            wrongFeedback.SetActive(false);

            Debug.Log(GameManager.Correct);
        }
        
        public void CheckAnswer(Text answer) /* check answer of the player */
        {
            if(isClickable)
            {
                if (GameManager.Correct == answer.text)
                {
                    correctFeedback.SetActive(true);
                    settings.SFX_Correct();
                    trophy.SetTrigger("add");
                }
                else
                {
                    wrongFeedback.SetActive(true);
                }

                //Debug.Log("answer count: " + answers.Count);
                //Debug.Log(string.Format("correct: {0}, answer: {1}", currentValue, int.Parse(answer)));

                StartCoroutine(UpdateItem());
                isClickable = false;
                isScalerNegative = true;          
            }
        }



        private IEnumerator UpdateItem() /* change item delayer adjust time based on the animation you will put in the game */
        {
            yield return new WaitForSeconds(1f);

             if (GameManager.Corrects.Count == 0)
              StartCoroutine( Feedback());
             else
              NextItem();
        }


        public void PlayAgain()
        {
            StartCoroutine(LoadNextScene());
        }

        public IEnumerator LoadNextScene()
        {
            GameObject.Find("Transition").GetComponent<Animator>().SetTrigger("in");
            yield return new WaitForSeconds(1f);

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Title");
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
            if (asyncOperation.isDone)
                Debug.Log(SceneManager.GetActiveScene().name + " | done");
        }

        private IEnumerator Feedback()
        {
            feedback.SetActive(true);

            // enable next difficulty when finished
            if (GameManager.Levels < 3)
                GameManager.Levels++;

            float percentage = (float) GameManager.Score / (float) GameManager.TotalItems;
            Debug.Log(percentage + " | score: " + GameManager.Score + " | total: " + GameManager.TotalItems);
            int stars = 0;

            if (percentage == 0)
                stars = 0;
            else if (percentage > 0 && percentage <= 0.4f)
                stars = 1;
            else if (percentage > 0.4f && percentage <= 0.7f)
                stars = 2;
            else if (percentage == 0.8f)
                stars = 3;
            else if (percentage == 0.9f)
                stars = 4;
            else
                stars = 5;

            if (percentage < 0.5f)
                feedbackText.text = "Subukan Muli.";
            else if(percentage >= 0.5f && percentage <= 0.7f)
                feedbackText.text = "Magaling!";
            else
                feedbackText.text = "Mahusay!";

            yield return new WaitForSeconds(0.6f);

            starAnimator.SetTrigger(stars.ToString());
            Debug.Log("stars:" + stars);
            feedbackScoreText.text = GameManager.Score.ToString();

            string difficulty;
            difficulty = GameManager.Difficulty.Replace(" ", "");

            if (difficulty.ToLower() == "level1")
            {
                diff = 1;
            }
            else if (difficulty.ToLower() == "level2")
            {
                diff = 2;
            }
            else
            {
                diff = 3;
            }
            Debug.Log(difficulty.ToLower() + " " + diff);
        }
    }
} 