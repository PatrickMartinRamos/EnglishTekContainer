using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace FilipinoTek.Grade2.ID102
{
    public class Settings : MonoBehaviour
    {
        private bool moreSettings = false;
        [SerializeField] Animator moreSettingsAnimator;
        [SerializeField] AudioClip correct, wrong, basket, shot;
        [SerializeField] AudioSource audioSource, soundEffects;
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private Slider MusicSlider, SFXSlider;

        public void SetMusicVolume()
        {
            float volume = MusicSlider.value;
            audioMixer.SetFloat("Music", Mathf.Log10(volume) * 20);
        }
        public void SetSFXVolume()
        {
            float volume = SFXSlider.value;
            audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
        }
        public void ShowMoreSettings()
        {
            moreSettings = !moreSettings;

            if (moreSettings) moreSettingsAnimator.SetTrigger("show");
            else moreSettingsAnimator.SetTrigger("hide");

        }

        public void MainSounds(Toggle t)
        {
            if(t.isOn)
                AudioListener.volume = 0;
            else
                AudioListener.volume = 1f;
        }

        public void SFX_Correct()
        {
            audioSource.clip = correct;
            audioSource.Play();
        }

        public void SFX_Wrong()
        {
            audioSource.clip = wrong;
            audioSource.Play();
        }

        public void SFX_BasketCount()
        {
            soundEffects.clip = basket;
            soundEffects.Play();
        }

        public void SFX_Shot()
        {
            soundEffects.clip = shot;
            soundEffects.Play();
        }
    }
}