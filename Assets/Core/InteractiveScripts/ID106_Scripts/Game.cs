using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace EnglishTek.Grade1.ID106
{
    public class Game : MonoBehaviour
    {
        public Text question;
        public Text score;
        public Text item;
        public Text instructions;

        public GameObject correct;
        public GameObject wrong;

        public Transform itemContainer;
        public GameObject[] flowers;
        public GameObject[] mushrooms;

        void Start()
        {
            instructions.text = GameManager.Instructions;
            NextItem();
        }

        void NextItem()
        {            
            GameManager.NextItem();
            question.text = GameManager.Question;
            item.text = GameManager.Item.ToString("00");
            score.text = GameManager.Score.ToString("00");

            for (int i = 0; i < itemContainer.childCount; i++) { Destroy(itemContainer.GetChild(i).gameObject); }
            FillFlowers();
            FillMushrooms();
        }

        void FillFlowers()
        {            
            for (int i = 0; i < 3; i++)
            { 
                var flower = Instantiate(flowers[Random.Range(0, flowers.Length)]);
                var x = Random.Range(-240f, 240f);
                var y = Random.Range(-140f, 25f);

                flower.transform.SetParent(itemContainer);
                flower.transform.localScale = Vector3.one;
                flower.transform.localPosition = new Vector3(x, y, 0f);
                flower.transform.name = "Flower";
                flower.GetComponent<Animator>().SetTrigger("idle");
            }
        }

        void FillMushrooms()
        {
            string[] _choices = GameManager.Choices.Split(',');

            for (int i = 0; i < 3; i++)
            {
                var mushroom = Instantiate(mushrooms[Random.Range(0, mushrooms .Length)]);
                var x = Random.Range(-280f, 280f);
                var y = Random.Range(-180f, 60f);

                mushroom.transform.SetParent(itemContainer);
                mushroom.transform.localScale = Vector3.one;
                mushroom.transform.localPosition = new Vector3(x, y, 0f);
                mushroom.transform.name = "Mushroom";
                mushroom.GetComponentInChildren<Text>().text = _choices[i];
                mushroom.GetComponent<Animator>().SetTrigger("idle");
            }
        }

        public void CheckAnswer(string answer)
        {
            foreach (Mushroom m in FindObjectsOfType<Mushroom>())
            {
                m.GetComponent<Button>().interactable = false;
            }

            bool isCorrect = GameManager.CheckAnswer(answer);
            if (isCorrect)
            {
                GameManager.Score++;
                score.text = GameManager.Score.ToString("00");

                correct.SetActive(true);
                correct.GetComponent<AudioSource>().Play();

                var flower = FindObjectsOfType<GameObject>().Where(obj => obj.name == "Flower");
                foreach (GameObject g in flower){g.GetComponent<Animator>().SetTrigger("correct");}

                var mushroom = FindObjectsOfType<GameObject>().Where(obj => obj.name == "Mushroom");
                foreach (GameObject g in mushroom)
                {
                    if (g.GetComponentInChildren<Text>().text == answer)
                        g.GetComponent<Animator>().SetTrigger("correct");  
                    else
                        g.GetComponent<Animator>().SetTrigger("hide");
                    g.GetComponentInChildren<Text>().gameObject.SetActive(false);
                }
            }
            else
            {
                wrong.SetActive(true);
                wrong.GetComponent<AudioSource>().Play();

                var flower = FindObjectsOfType<GameObject>().Where(obj => obj.name == "Flower");
                foreach (GameObject g in flower) { g.GetComponent<Animator>().SetTrigger("wrong"); }

                var mushroom = FindObjectsOfType<GameObject>().Where(obj => obj.name == "Mushroom");
                foreach (GameObject g in mushroom)
                {                    
                    if (g.GetComponentInChildren<Text>().text == answer)
                        g.GetComponent<Animator>().SetTrigger("wrong");
                    else
                        g.GetComponent<Animator>().SetTrigger("hide");
                    g.GetComponentInChildren<Text>().gameObject.SetActive(false);
                }
            }

            StartCoroutine(AnswerFeedback(isCorrect));
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
