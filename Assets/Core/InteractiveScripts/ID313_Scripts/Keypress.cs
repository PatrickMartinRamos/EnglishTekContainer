using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace EnglishTek.Grade1.ID313
{
    public class Keypress : MonoBehaviour {
    	public GameObject keyboard,capslock;
    	private InputField input;
    	public bool caps;
    	// Use this for initialization

    	public void Selectfield(InputField selected){
            #if !UNITY_EDITOR && UNITY_WEBGL
               if (platformCheck () == "mobile") {
    				keyboard.SetActive (true);
    				input = selected;
    			}
           #endif
    	}

    	//USE IF TYPING ONLY 1 LETTER
    	public void Typeletter(string letter){
    		if (caps == false) {
    			input.text = letter.ToLower ();
    		} else if (caps == true) {	
    			input.text = letter.ToUpper ();
    		}
    		//keyboard.SetActive (false);
    	}

    	//USE IF TYPING A WORD
    	public void TypeWord(string letter){
    		string temp;
    		temp = input.text;
    		if (caps == false) {
    			input.text = temp + letter.ToLower ();
    		} else if (caps == true) {	
    			input.text = temp + letter.ToUpper ();
    		}

    		//keyboard.SetActive (false);
    	}

    	//CLEAR TEXT
    	public void Cleartxt(){
    		input.text = "";
    	}
    	public void Deletetxt(){
    		string tempstring;
    		int length;
    		tempstring = input.text;
    		length = tempstring.Length-1;
    		if (input.text!="") {
    			tempstring = input.text.Remove (length);
    			input.text = tempstring;
    		}
    	}


    	public void Caps(){
    		if (caps == false) {
    			caps = true;
    			capslock.GetComponent<Image> ().color = Color.green;
    		} else if (caps == true) {
    			caps = false;
    			capslock.GetComponent<Image> ().color = Color.white;
    		}
    	}
    	#if !UNITY_EDITOR && UNITY_WEBGL
	[DllImport("__Internal")]
    	private static extern string platformCheck();
	#endif

    }
}