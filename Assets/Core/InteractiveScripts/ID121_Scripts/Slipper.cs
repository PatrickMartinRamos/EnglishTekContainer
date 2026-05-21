using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishTek.Grade1.ID121
{
    public class Slipper : MonoBehaviour
    {
        public Transform indicator;
        public Rigidbody2D rb;

        public Slider power;

        public bool Move { set; get; }
        public bool Aim { set; get; }
        public bool Shoot { set; get; }
        public bool Hit { set; get; }

        Game game;
        float force = 30f;
        string powerDirection = "up";
        Vector3 startPos;

        void Start()
        {
            game = FindObjectOfType<Game>();
            startPos = new Vector3(0f, -230f, 0f);
        }

        public void Initialize()
        {
            Move = false;
            Aim = false;
            Shoot = false;
            Hit = false;
            power.value = 0f;
            rb.velocity = Vector2.zero;
            transform.localPosition = startPos;
            transform.localEulerAngles = Vector3.zero;
        }

        void Update()
        {
            if (Move)
            {
                if (!Aim)
                {
                    float x = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
                    Vector3 position = new Vector3(x, transform.position.y, 0f);
                    transform.position = position;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    Shoot = true;
                    Aim = true;
                    InvokeRepeating("PowerGauge", 0.01f, 0.01f);
                }

                if (Input.GetMouseButton(0))
                {
                    if (Aim)
                    {
                        if (!indicator.gameObject.activeSelf)
                        {
                            indicator.gameObject.SetActive(true);
                        }

                        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        float angleRad = Mathf.Atan2(mouse.y - transform.position.y, mouse.x - transform.position.x);
                        float angleDeg = (180 / Mathf.PI) * angleRad;
                        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (!Shoot) return;

                    Move = false;
                    CancelInvoke("PowerGauge");
                    indicator.gameObject.SetActive(false);
                    rb.AddRelativeForce(Vector2.up * power.value * force, ForceMode2D.Impulse);
                    rb.AddTorque(180f);

                    Invoke("Next", 4f); // Timer before checking for a miss
                }
            }
        }

        void Next()
        {
            // Removed: game.CheckAnswer("wrong");
            // Instead of marking it wrong, we reset the slipper for another try.

            Aim = false;
            Shoot = false;
            Hit = false;
            power.value = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f; // Added this to stop the slipper from spinning when it resets
            transform.localPosition = startPos;
            transform.localEulerAngles = Vector3.zero;
            Move = true; // Give control back to the player
        }

        void PowerGauge()
        {
            if (powerDirection == "up")
            {
                power.value += 0.01f;

                if (power.value >= 1f)
                    powerDirection = "down";
            }
            if (powerDirection == "down")
            {
                power.value -= 0.01f;

                if (power.value <= 0f)
                    powerDirection = "up";
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var name = collision.gameObject.name;

            if (name == "Can")
            {
                if (Hit) return;

                Vector3 hit = collision.contacts[0].point;

                collision.gameObject.transform.Find("HitEffect").position = hit;
                collision.gameObject.GetComponent<Animator>().SetTrigger("hit");
                game.CheckAnswer(collision.gameObject.GetComponentInChildren<Text>().text);

                Hit = true;
                CancelInvoke("Next");
            }
        }
    }
}