using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Security.AccessControl;
using System.Runtime.InteropServices;

namespace EnglishTek.Grade2.ID232
{
    public class Game : MonoBehaviour
    {
        public Text question;
        public Text score;
        public Text item;
        public Text instructions;
		public Text feedbackText;
		public Text timeTxt;
        public Text[] choices;

        public Button helpButton;

        public GameObject correct;
        public GameObject wrong;
		public GameObject questionBox;
		public GameObject FeedbackBox;
		public GameObject TimesUp;

		public GameObject scientist_idle;
		public GameObject scientist_accept;
		public GameObject scientist_declined;
		public GameObject robot_entrance;
		public GameObject robot_accept;
		public GameObject robot_declined;
        
        public GameObject life1;
		public GameObject life2;
		public GameObject life3;

		bool isClick,Intro;
		public string valClick;

		float currCountdownValue;
		float difficultyTime;

		int life;

		private bool cLeft = false,cRight=false;
		public GameObject ArrowKeys;
		void Awake () {
			if (GameManager.Difficulty == "Practice") {
				difficultyTime = 20;
			} else if (GameManager.Difficulty == "Workout") {
				difficultyTime = 15;
			} else if (GameManager.Difficulty == "Quiz") {
				difficultyTime = 10;
			}

			life = 3;

			currCountdownValue = difficultyTime;
			if(currCountdownValue < 10)
				timeTxt.text = "0:0" + currCountdownValue.ToString ();
			else timeTxt.text = "0:" + currCountdownValue.ToString ();
		}

        void Start()
        {

			#if !UNITY_EDITOR && UNITY_WEBGL
            if (platformCheck () == "mobile") {
				ArrowKeys.SetActive (true);
			}
			#endif
			instructions.text = GameManager.Instructions;
			NextItemFirst ();
			//StartCoroutine (NextItem ());

        }

		void NextItemFirst () {
			GameManager.NextItem();
			question.text = GameManager.Question;
            question.text = question.text.Replace("(", "<b>");
            question.text = question.text.Replace(")", "</b>");
            item.text = GameManager.Item.ToString("00");
			score.text = GameManager.Score.ToString("00");
            string[] _choices = GameManager.Choices.Split(',');
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i].gameObject.SetActive(true);
                choices[i].GetComponentInChildren<Text>().text = _choices[i];
            }
            robot_entrance.SetActive (true);
			robot_accept.SetActive (false);
			robot_declined.SetActive (false);

			scientist_idle.SetActive (true);
			scientist_accept.SetActive (false);
			scientist_declined.SetActive (false);

			FeedbackBox.SetActive (false);

			StartCoroutine(showQuestionBubble());
			StartCoroutine(WaitIntro());
		}


		IEnumerator NextItem()
        { 
			yield return new WaitForSeconds(2f);

            GameManager.NextItem();
            question.text = GameManager.Question;
            question.text = question.text.Replace("(", "<b>");
            question.text = question.text.Replace(")", "</b>");
            item.text = GameManager.Item.ToString("00");
            score.text = GameManager.Score.ToString("00");
            string[] _choices = GameManager.Choices.Split(',');
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i].gameObject.SetActive(true);
                choices[i].GetComponentInChildren<Text>().text = _choices[i];
            }
            TimesUp.SetActive (false);
			helpButton.enabled = true;

			robot_entrance.SetActive (true);
			robot_accept.SetActive (false);
			robot_declined.SetActive (false);

			scientist_idle.SetActive (true);
			scientist_accept.SetActive (false);
			scientist_declined.SetActive (false);

			FeedbackBox.SetActive (false);

			currCountdownValue = difficultyTime;
			if(currCountdownValue < 10)
				timeTxt.text = "0:0" + currCountdownValue.ToString ();
			else timeTxt.text = "0:" + currCountdownValue.ToString ();

			StartCoroutine(showQuestionBubble());
			StartCoroutine(WaitIntro());

		}

		public IEnumerator StartCountdown(float countdownValue)
		{
			currCountdownValue = countdownValue;
//			if(currCountdownValue < 10)
//				timeTxt.text = "0:0" + currCountdownValue.ToString ();
//			else timeTxt.text = "0:" + currCountdownValue.ToString ();

			while (currCountdownValue > 0 && isClick) {
				yield return new WaitForSeconds (1.0f);
				currCountdownValue--;
				if (currCountdownValue < 10) timeTxt.text = "0:0" + currCountdownValue.ToString ();
				else timeTxt.text = "0:" + currCountdownValue.ToString ();
			} if (currCountdownValue <= 0 && isClick) {
				

				TimesUp.SetActive (true);
				isClick = false;

				questionBox.SetActive (false);
				robot_entrance.SetActive (false);

				life -= 1;

				if (life == 2) life1.SetActive (false);
				else if (life == 1) life2.SetActive (false);
				else if(life < 1) life3.SetActive(false);

				if (GameManager.Item >= GameManager.TotalItem)
					SceneManager.LoadScene ("Feedback");
				else
					StartCoroutine (NextItem ());   
				
				if (life < 1) {
					StartCoroutine (DelayFeedback ());
				} 

			}
		}


        public void CheckAnswer(string answer)
        {    

			bool isCorrect = GameManager.CheckAnswer(answer);
            print("Answer mo: " + answer + ".");
            print("Correct Answer: " + GameManager.Correct + ".");
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

				life -= 1;

				if (life == 2) life1.SetActive (false);
				else if (life == 1) life2.SetActive (false);
				else if(life < 1) life3.SetActive(false);
			}

			StartCoroutine(AnswerFeedback(isCorrect));

			if (life < 1) {
				StartCoroutine (DelayFeedback ());
			} 
        }


		IEnumerator DelayFeedback() {
			yield return new WaitForSeconds (1.8f);

			SceneManager.LoadScene ("Feedback");
		}

		void Update()
		{
			if (isClick==true) 
			{
				if (Input.GetKey(KeyCode.LeftArrow)||cLeft==true)
				{
					scientist_idle.SetActive (false);
					scientist_accept.SetActive (true);
                    choices[0].gameObject.SetActive(false);
                    choices[1].gameObject.SetActive(false);
                    robot_entrance.SetActive (false);
					robot_accept.SetActive (true);

					isClick = false;
					questionBox.SetActive (false);

					CheckAnswer (choices[0].text);
					valClick = "left";

					helpButton.enabled = false;
					cLeft = false;
					Intro = false;
				}

				if (Input.GetKey(KeyCode.RightArrow)||cRight==true)
				{
					scientist_idle.SetActive (false);
					scientist_declined.SetActive (true);
                    choices[0].gameObject.SetActive(false);
                    choices[1].gameObject.SetActive(false);
                    robot_entrance.SetActive (false);
					robot_declined.SetActive (true);

					isClick = false;
					questionBox.SetActive (false);

					CheckAnswer (choices[1].text);
					valClick = "right";

					helpButton.enabled = false;
					cRight = false;
					Intro = false;
				}

				
			}

		}

		IEnumerator showQuestionBubble()
		{  
			yield return new WaitForSeconds(2f);
			questionBox.SetActive (true);
			isClick = true;

			StartCoroutine(StartCountdown(difficultyTime));
			print (GameManager.Correct);
		}

		IEnumerator showFeedbackText()
		{  
			yield return new WaitForSeconds(0.5f);

			FeedbackBox.SetActive (true);
		}

        IEnumerator AnswerFeedback(bool isCorrect)
        {            
            yield return new WaitForSeconds(2f); // orig | yield return new WaitForSeconds(7f);

			if (isCorrect) correct.SetActive (false);
			else {
				wrong.SetActive(false);
				if(valClick == "right") feedbackText.text = "Oops, you threw out a good quality robot!";
				else if(valClick == "left") feedbackText.text = "Oh, no! The robot is faulty.";

				FeedbackBox.SetActive (true);
				//StartCoroutine (showFeedbackText ());
			}

            if (GameManager.Item >= GameManager.TotalItem)
                SceneManager.LoadScene("Feedback");
            else
				StartCoroutine (NextItem ());     
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
			isClick = false;
        }

		public void CloseIns() {
			isClick = true;
			StartCoroutine (StartCountdown (currCountdownValue));
		}
		public void BtnLeft()
        {
			if (Intro == true)
            {
				cLeft = true;
			}
			
			
        }
		public void BtnRight()
        {
			if (Intro == true)
			{
				cRight = true;
			}
			
			
		}
		IEnumerator WaitIntro()
        {
			yield return new WaitForSeconds(2f);
			Intro = true;
        }
#if UNITY_WEBGL
		[DllImport("__Internal")]
		private static extern string platformCheck();
#endif
	}
}
