using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FilipinoTek.Grade2.ID101
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

        public List<Button> repeat_button;

        [SerializeField] GameObject correctFeedback, wrongFeedback;
        private List<int> answers;

        private int diff = 0;
        private bool isClickable;

        public GameObject MainContainer, SoundContainer;

        public List<GameObject> Dialouge;
        [SerializeField] List<Text> DialougeText;


        public GameObject Next, AnsQuestion, liham;

        int CurDialouge;

        public bool BgMove;

        [SerializeField] List<AudioClip> sound_effect;
        [SerializeField] AudioSource sound_effect_audiosource;

        [SerializeField] GameObject choicesbutton2;
        [SerializeField] GameObject choicesbutton3;
        [SerializeField] GameObject choicesbutton4;

        [SerializeField] List<Button> choicesSet2;
        [SerializeField] List<Button> choicesSet3;
        [SerializeField] List<Button> choicesSet4;

        [SerializeField] List<Button> choicesButton;

        public SubmitScore ss;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            GameManager.Initialize();

            if (itemsParent.childCount != 0)
            {
                for (int i = 0; i < itemsParent.childCount; i++)
                {
                    Destroy(itemsParent.GetChild(i).gameObject);
                }
            }

            for (int i = 0; i < GameManager.TotalItems; i++)
            {
                GameObject item = Instantiate(itemPrefab);
                item.name = "Item";
                item.transform.SetParent(itemsParent);
                item.transform.localScale = Vector3.one;
            }

            CurDialouge = 0;
            DialougeText[CurDialouge].text = GameManager.Dialouges[CurDialouge];

            BgMove = false;
            NextItem();

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

        public void RepeatButton_Validator_OFF ()
        {
            for (int i = 0; i < repeat_button.Count; i++)
            {

                repeat_button[i].interactable = false;
            }

            StartCoroutine(RepeatButton_Validator_ON());
        }

        private IEnumerator RepeatButton_Validator_ON ()
        {
            yield return new WaitForSeconds(9f);

            for (int i = 0; i < repeat_button.Count; i++)
            {
                repeat_button[i].interactable = true;
            }
        }


        private void SoundValidator()
        {
            for (int i = 0; i < CurDialouge - 1; i++)
            {
                Dialouge[i].GetComponentInChildren<AudioSource>().Stop();
            }
        }

       
        public void ShowDialogue () 
        {
            BgMove = true;
            CurDialouge += 1;

            // Activate the current dialogue
            if (CurDialouge - 1 < Dialouge.Count)
            {
                Dialouge[CurDialouge - 1].SetActive(true);
            }

            // Show answer options only on the last dialogue
            if (CurDialouge == Dialouge.Count)
            {
                Next.SetActive(false);
                AnsQuestion.SetActive(true);
            }
            else
            {
                Next.SetActive(true);
                AnsQuestion.SetActive(false);
            }

            RepeatButton_Validator_OFF();
            SoundValidator();

            // Update dialogue text
            if (CurDialouge - 1 < DialougeText.Count && CurDialouge - 1 < GameManager.Dialouges.Count)
            {
                DialougeText[CurDialouge - 1].text = GameManager.Dialouges[CurDialouge - 1];
            }

        }



        public void ValidateCurrentDialouge ()
        {
            SoundContainer.SetActive(false);
            MainContainer.SetActive(true);

            BgMove = false;
      
        }

        public void MuteSound()
        {

           if (CurDialouge == Dialouge.Count)
            {
                // Stop all audio sources in all dialogues
                foreach (var dial in Dialouge)
                {
                    var audio = dial.GetComponentInChildren<AudioSource>();
                    if (audio != null)
                        audio.Stop();
                }
            }
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

                answers = new List<int>();
                foreach (var item in itemsParent.GetComponentsInChildren<Toggle>())
                {
                    if (!item.isOn)
                    {
                        item.isOn = true;
                        break;
                    }
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

                answers = new List<int>();
                foreach (var item in itemsParent.GetComponentsInChildren<Toggle>())
                {
                    if (!item.isOn)
                    {
                        item.isOn = true;
                        break;
                    }
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

                answers = new List<int>();
                foreach (var item in itemsParent.GetComponentsInChildren<Toggle>())
                {
                    if (!item.isOn)
                    {
                        item.isOn = true;
                        break;
                    }
                }
            }


            isClickable = true;
            correctFeedback.SetActive(false);
            wrongFeedback.SetActive(false);

            MuteSound();
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

                GameManager.CurItem += 1;

                StartCoroutine(UpdateItem());

    

                isClickable = false;
            }
        }


        private IEnumerator FeedbackDelayer()
        {
            yield return new WaitForSeconds(1.5f);

            StartCoroutine(Feedback());
        }

        private IEnumerator UpdateItem() /* change item delayer adjust time based on the animation you will put in the game */
        {
            yield return new WaitForSeconds(1f);

             if (GameManager.CurItem > 5)
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
            else if (percentage > 0 && percentage <= 0.3f)
                stars = 1;
            else if (percentage > 0.3f && percentage <= 0.7f)
                stars = 2;
            else if (percentage == 0.8f)
                stars = 3;
            else if (percentage == 0.9f)
                stars = 4;
            else
                stars = 5;

            if (percentage < 0.6f)
                feedbackText.text = "Subukan Muli.";
            else if (percentage >= 0.6f && percentage <= 0.9f)
                feedbackText.text = "Magaling!";
            else if (percentage >= 1f)
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
            StartCoroutine(ss.PostScores(diff, GameManager.Score)); 
        }
    }
} 