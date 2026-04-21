using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml;
using UnityEngine.EventSystems;

namespace FilipinoTek.Grade2.ID101
{
    public class Title : MonoBehaviour
    {
        [SerializeField] Button play;
        //[SerializeField] Text instructions;
        [SerializeField] Toggle[] levels;
        [SerializeField] List<AudioClip> sound_effect;
        [SerializeField] AudioSource sound_effect_audiosource;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            UnlockLevels();

        }

        private void UnlockLevels()
        {
            if (GameManager.Levels == 0)
                GameManager.Levels = 1;

            for (int i = 0; i < GameManager.Levels; i++)
            {
                levels[i].interactable = true;
                var eventTrigger = levels[i].GetComponent<EventTrigger>();
                eventTrigger.enabled = true;
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

        public void DifficultySelect(Toggle t)
        {
            if (t.isOn)
            {
                t.transform.Find("Label").gameObject.SetActive(false);
                GameManager.Difficulty = t.GetComponentInChildren<Text>().text;

                if (!play.interactable)
                    play.interactable = true;

                if (t.group.allowSwitchOff)
                    t.group.allowSwitchOff = false;
            }
            else
            {
                t.transform.Find("Label").gameObject.SetActive(true);
            }
        }

        public void Next()
        {
            if (GameManager.Difficulty == "")
                return;
            StartCoroutine(LoadNextScene());
        }

        IEnumerator LoadNextScene()
        {
            //GameObject.Find("Transition").GetComponent<Animator>().SetTrigger("in");
            yield return new WaitForSeconds(0.5f);

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Game");
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
            if (asyncOperation.isDone)
                Debug.Log(SceneManager.GetActiveScene().name + " | done");
        }
    }
}