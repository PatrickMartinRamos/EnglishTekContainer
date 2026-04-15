using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EnglishTek.Grade1.ID106
{
    public class Hammer : MonoBehaviour
    {
        Animator animator;
        Game game;

        void Start()
        {
            animator = GetComponent<Animator>();
            game = FindObjectOfType<Game>();
        }

        void Update()
        {       
            if (Input.GetMouseButtonDown(0))
            {
                animator.SetTrigger("hit");
            }

            Vector3 clampedPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, -350f, 350f);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, -195f, 180f);
            clampedPosition.z = 0f;
            transform.position = clampedPosition;
        }
    }
}