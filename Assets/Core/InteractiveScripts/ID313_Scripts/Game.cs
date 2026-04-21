using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace EnglishTek.Grade1.ID313
{
    public class Game : MonoBehaviour
    {
        public Text question;
        public Text score;
        public Text item;
        public Text instructions;
        public InputField iField;
        public Button submits;
        string answerTxt, sTex= "on";
        public GameObject correct,rope,peng1,peng2;
        public GameObject wrong;
        public Animator ropewin, ropelose, peng1win, peng1lose, peng2win, peng2lose;
        void Start()
        {
            answerTxt = "";
            instructions.text = GameManager.Instructions;
            NextItem();
        }

        void NextItem()
        {            
            GameManager.NextItem();
            iField.text = "";
            iField.interactable = true;
            //iField.ActivateInputField();
           // iField.Select(); 
            question.text = GameManager.Question;
            item.text = GameManager.Item.ToString("00");
            score.text = GameManager.Score.ToString("00");
            rope.GetComponent<Image>().enabled = true;
            peng1.GetComponent<Image>().enabled = true;
            peng2.GetComponent<Image>().enabled = true;
            submits.interactable = true;
            ropewin.gameObject.SetActive(false);
            ropelose.gameObject.SetActive(false);
            peng1lose.gameObject.SetActive(false);
            peng1win.gameObject.SetActive(false);
            peng2win.gameObject.SetActive(false);
            peng2lose.gameObject.SetActive(false);
            ropewin.SetTrigger("idle");
            ropelose.SetTrigger("idle");
            peng1lose.SetTrigger("idle");
            peng1win.SetTrigger("idle");
            peng2lose.SetTrigger("idle");
            peng2win.SetTrigger("idle");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                
            }
        }

        public void SubmitPress()
        {
            if (iField.text != "")
            {
                iField.interactable = false;
                submits.interactable = false;
                CheckAnswer(iField);
                iField.text = "";
            }

        }

        public void CheckAnswer(InputField answer)
        {
            answerTxt = answer.text;
            if(answerTxt != "") { 
            answer.interactable = false;
            bool isCorrect = GameManager.CheckAnswer(answerTxt);
            if (isCorrect)
            {
                GameManager.Score++;
                score.text = GameManager.Score.ToString("00");

                rope.GetComponent<Image>().enabled = false;
                peng1.GetComponent<Image>().enabled = false;
                peng2.GetComponent<Image>().enabled = false;
                ropewin.gameObject.SetActive(true);
                peng1win.gameObject.SetActive(true);
                peng2lose.gameObject.SetActive(true);
                ropewin.SetTrigger("win");
                peng1win.SetTrigger("win");
                peng2lose.SetTrigger("lose");

                correct.SetActive(true);
                    var sounds = FindObjectsOfType<AudioSource>();
                    if (sTex == "off")
                    {
                        for (int x = 0; x < sounds.Length; x++)
                        {
                            sounds[x].volume = 0f;
                        }
                        
                    } else if (sTex == "on")
                    {
                        for (int x = 0; x < sounds.Length; x++)
                        {
                            sounds[x].volume = 1f;
                        }
                    }
                    print(sTex);
                    correct.GetComponent<AudioSource>().Play();


                }
            else
            {
                wrong.SetActive(true);
                rope.GetComponent<Image>().enabled = false;
                peng1.GetComponent<Image>().enabled = false;
                peng2.GetComponent<Image>().enabled = false;
                ropelose.gameObject.SetActive(true);
                peng1lose.gameObject.SetActive(true);
                peng2win.gameObject.SetActive(true);
                ropelose.SetTrigger("lose");
                peng1lose.SetTrigger("lose");
                peng2win.SetTrigger("win");
                    var sounds = FindObjectsOfType<AudioSource>();
                    if (sTex == "on")
                    {
                        for (int x = 0; x < sounds.Length; x++)
                        {
                            sounds[x].volume = 1f;
                        }
                    } else if (sTex == "off")
                    {
                        for (int x = 0; x < sounds.Length; x++)
                        {
                            sounds[x].volume = 0f;
                        }

                    }
                    wrong.GetComponent<AudioSource>().Play();
            }
                StartCoroutine(AnswerFeedback(isCorrect));
            }
        }

        IEnumerator AnswerFeedback(bool isCorrect)
        {            
            yield return new WaitForSeconds(3f); // orig | yield return new WaitForSeconds(7f);

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
            var sounds = FindObjectsOfType<AudioSource>();
            if (soundText.text == "SOUND ON")
            {
                soundText.text = "SOUND OFF";
                sTex = "off";
               // listener.enabled = false;
               for(int x = 0; x < sounds.Length; x++)
                {
                    sounds[x].volume = 0f;
                }
            }
            else
            {
                soundText.text = "SOUND ON";
                sTex = "on";
                //listener.enabled = true;
                for (int x = 0; x < sounds.Length; x++)
                {
                    sounds[x].volume = 1f;
                }
            }
            
        }

        public void Instructions()
        {
            
        }
    }
}
