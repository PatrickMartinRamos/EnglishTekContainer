using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnglishTek.Grade1.ID213
{
    public class Character : MonoBehaviour
    {
        bool horizontal = false;
        bool vertical = false;
        bool move = false;
        Animator character;
        float speed = 2f;
        Game game;
        float X = 0, Y = 0;
        bool moveAnimation = false;

        // Track if movement is from UI buttons or keyboard
        bool usingKeyboard = false;

        // Track previous movement state to detect when stopping
        float prevX = 0, prevY = 0;

        void Start()
        {
            character = GetComponent<Animator>();
            game = FindObjectOfType<Game>();
        }

        public void Move(bool value)
        {
            move = value;
        }

        public void Initialize()
        {
            transform.localPosition = new Vector3(0f, 140f, -0.1f);
            move = true;
        }

        void Update()
        {
            if (move)
            {
                // Handle keyboard input (Arrow keys only, excluding WASD)
                HandleKeyboardInput();

                if (moveAnimation)
                {
                    if (X == -1)
                        character.SetTrigger("left");
                    else if (X == 1)
                        character.SetTrigger("right");
                    else if (Y == 1)
                        character.SetTrigger("back");
                    else if (Y == -1)
                        character.SetTrigger("front");
                    moveAnimation = false;
                }

                // Check if character stopped moving
                if ((prevX != 0 || prevY != 0) && X == 0 && Y == 0)
                {
                    character.SetTrigger("idle");
                }

                // Update previous movement values
                prevX = X;
                prevY = Y;

                Vector3 moveDirection = new Vector3(X, Y, 0f);
                transform.Translate(moveDirection * speed * Time.deltaTime);
                Vector3 clamp = transform.localPosition;
                clamp.x = Mathf.Clamp(transform.localPosition.x, -375f, 375f);
                clamp.y = Mathf.Clamp(transform.localPosition.y, -280f, 145f);
                transform.localPosition = clamp;
            }
        }

        void HandleKeyboardInput()
        {
            // Check for arrow key input
            bool leftArrow = Input.GetKey(KeyCode.LeftArrow);
            bool rightArrow = Input.GetKey(KeyCode.RightArrow);
            bool upArrow = Input.GetKey(KeyCode.UpArrow);
            bool downArrow = Input.GetKey(KeyCode.DownArrow);

            // Only process keyboard input if any arrow key is pressed
            if (leftArrow || rightArrow || upArrow || downArrow)
            {
                usingKeyboard = true;

                // Handle horizontal movement
                if (leftArrow && !rightArrow)
                {
                    if (X != -1)
                    {
                        X = -1;
                        moveAnimation = true;
                    }
                }
                else if (rightArrow && !leftArrow)
                {
                    if (X != 1)
                    {
                        X = 1;
                        moveAnimation = true;
                    }
                }
                else
                {
                    X = 0;
                }

                // Handle vertical movement
                if (upArrow && !downArrow)
                {
                    if (Y != 1)
                    {
                        Y = 1;
                        moveAnimation = true;
                    }
                }
                else if (downArrow && !upArrow)
                {
                    if (Y != -1)
                    {
                        Y = -1;
                        moveAnimation = true;
                    }
                }
                else
                {
                    Y = 0;
                }
            }
            else if (usingKeyboard)
            {
                // Reset when no arrow keys are pressed (only if we were using keyboard)
                X = 0;
                Y = 0;
                usingKeyboard = false;
            }
        }

        void OnDrawGizmos()
        {
            float z = -0.1f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(0f, -67.5f, z), new Vector3(450f, 425f, 0f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(-225f, -280f, z), new Vector3(-225f, 145f, z));
            Gizmos.DrawLine(new Vector3(225f, -280f, z), new Vector3(225f, 145f, z));
        }

        public void VerticalButtonsDown(float value)
        {
            Y = value;
            moveAnimation = true;
            usingKeyboard = false;
        }

        public void HorizontalButtonsDown(float value)
        {
            X = value;
            moveAnimation = true;
            usingKeyboard = false;
        }

        public void AllPointerUp()
        {
            if (!usingKeyboard)
            {
                X = 0f;
                Y = 0f;
            }
        }

        void OnTriggerEnter2D(Collider2D collider)
        {
            string answer = string.Empty;

            // Get answer based on which object was collided with
            if (collider.gameObject.name == "starfish")
            {
                answer = game.GetStarfishAnswer();
            }
            else if (collider.gameObject.name == "shell")
            {
                answer = game.GetShellAnswer();
            }

            // Only process if we got a valid answer
            if (!string.IsNullOrEmpty(answer))
            {
                move = false;
                character.SetTrigger("idle");
                game.CheckAnswer(answer);
            }
        }
    }
}