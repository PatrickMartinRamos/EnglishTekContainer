using UnityEngine;

namespace EnglishTek.Core
{
    [RequireComponent(typeof(Camera))]
    public class AspectRatioEnforcer : MonoBehaviour
    {
        [SerializeField] private float targetAspectWidth = 16f;
        [SerializeField] private float targetAspectHeight = 9f;

        private float lastScreenWidth;
        private float lastScreenHeight;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (!Mathf.Approximately(Screen.width, lastScreenWidth) ||
                !Mathf.Approximately(Screen.height, lastScreenHeight))
            {
                Apply();
            }
        }

        private void Apply()
        {
            lastScreenWidth  = Screen.width;
            lastScreenHeight = Screen.height;

            float targetAspect = targetAspectWidth / targetAspectHeight;
            float screenAspect = (float)Screen.width / Screen.height;
            float scale = screenAspect / targetAspect;

            Camera cam = GetComponent<Camera>();
            cam.backgroundColor = Color.black;

            if (Mathf.Approximately(scale, 1f))
            {
                // Perfect match — full viewport
                cam.rect = new Rect(0f, 0f, 1f, 1f);
            }
            else if (scale < 1f)
            {
                // Screen is taller than target — pillarbox (black bars top/bottom)
                float offsetY = (1f - scale) / 2f;
                cam.rect = new Rect(0f, offsetY, 1f, scale);
            }
            else
            {
                // Screen is wider than target — letterbox (black bars left/right)
                float scaleWidth = 1f / scale;
                float offsetX = (1f - scaleWidth) / 2f;
                cam.rect = new Rect(offsetX, 0f, scaleWidth, 1f);
            }

            Debug.Log("[AspectRatio] Screen: " + Screen.width + "x" + Screen.height +
                      " | Scale: " + scale.ToString("F3") +
                      " | Rect: " + cam.rect);
        }
    }
}
