using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EnglishTek.Core
{
    public enum OverlayButtonCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    [DisallowMultipleComponent]
    public class ContainerReturnOverlay : MonoBehaviour
    {
        // --- Prefab mode ---
        [SerializeField] private Button backButton = null;

        // --- Procedural fallback (used only when backButton is not assigned) ---
        [SerializeField] private string backButtonLabel = "< Menu";
        [SerializeField] private Vector2 buttonSize = new Vector2(120f, 44f);
        [SerializeField] private Vector2 buttonPadding = new Vector2(10f, 10f);
        [SerializeField] private OverlayButtonCorner buttonCorner = OverlayButtonCorner.TopLeft;
        [SerializeField] private Color buttonColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color labelColor = Color.white;
        [SerializeField] private int labelFontSize = 18;

        private static ContainerReturnOverlay instance;
        private GameObject canvasRoot;
        private GameObject visibilityTarget;  // the object show/hide — never the root GO
        private bool lastVisible;

        // Static pending settings written by EnsureExists before AddComponent,
        // so Awake can read the correct values when BuildCanvas runs.
        private static OverlayButtonCorner s_pendingCorner = OverlayButtonCorner.TopLeft;
        private static Vector2 s_pendingPadding = new Vector2(10f, 10f);

        public static void EnsureExists(ContainerReturnOverlay prefab = null,
            OverlayButtonCorner corner = OverlayButtonCorner.TopLeft,
            Vector2 padding = default)
        {
            if (instance != null)
            {
                instance.ApplySettings(corner, padding == default ? new Vector2(10f, 10f) : padding);
                return;
            }

            if (prefab != null)
            {
                ContainerReturnOverlay spawned = Instantiate(prefab);
                DontDestroyOnLoad(spawned.gameObject);
                // Apply after Awake has run so instance is set and backButton is wired up.
                if (instance != null)
                {
                    instance.ApplySettings(corner, padding == default ? new Vector2(10f, 10f) : padding);
                }
                return;
            }

            // Set before AddComponent so Awake reads them inside BuildCanvas.
            s_pendingCorner = corner;
            s_pendingPadding = padding == default ? new Vector2(10f, 10f) : padding;

            GameObject go = new GameObject("ContainerReturnOverlay");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ContainerReturnOverlay>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (backButton != null)
            {
                backButton.onClick.AddListener(ReturnToContainer);
                canvasRoot = gameObject;
                // Show/hide the button itself, not the root, so Update keeps running.
                visibilityTarget = backButton.gameObject;
            }
            else
            {
                // Apply settings passed from EnsureExists before building.
                buttonCorner = s_pendingCorner;
                buttonPadding = s_pendingPadding;
                BuildCanvas();
                visibilityTarget = canvasRoot;
            }

            // Start hidden; Update will show it when the active scene is Title.
            SetVisible(false);
            lastVisible = false;
        }

        private void Update()
        {
            bool isTitle = string.Equals(
                SceneManager.GetActiveScene().name, "Title",
                System.StringComparison.OrdinalIgnoreCase);

            if (isTitle == lastVisible)
            {
                return;
            }

            lastVisible = isTitle;
            SetVisible(isTitle);
        }

        private void SetVisible(bool visible)
        {
            if (visibilityTarget != null)
            {
                visibilityTarget.SetActive(visible);
            }
        }

        private void ReturnToContainer()
        {
            string containerScene = GameSession.ContainerSceneName;
            SetVisible(false);
            GameSession.CleanUp();

            if (!string.IsNullOrEmpty(containerScene))
            {
                SceneManager.LoadScene(containerScene, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("ContainerReturnOverlay: ContainerSceneName is not set. Cannot navigate back.");
            }
        }

        private void ApplySettings(OverlayButtonCorner corner, Vector2 padding)
        {
            buttonCorner = corner;
            buttonPadding = padding;

            // If using a prefab-assigned button, reposition its RectTransform directly.
            if (backButton != null)
            {
                RectTransform rt = backButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    ApplyCorner(rt, buttonCorner, buttonPadding, rt.sizeDelta);
                }
                return;
            }

            // Procedural path — find the BackButton child and reposition it.
            if (canvasRoot == null) { return; }
            Transform buttonTransform = canvasRoot.transform.Find("BackButton");
            if (buttonTransform != null)
            {
                RectTransform rt = buttonTransform.GetComponent<RectTransform>();
                if (rt != null)
                {
                    ApplyCorner(rt, buttonCorner, buttonPadding, buttonSize);
                }
            }
        }

        private void BuildCanvas()
        {
            canvasRoot = new GameObject("ContainerReturnCanvas", typeof(RectTransform));
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            canvasRoot.AddComponent<GraphicRaycaster>();

            // Button
            GameObject buttonObject = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasRoot.transform, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            ApplyCorner(buttonRect, buttonCorner, buttonPadding, buttonSize);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = buttonColor;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonImage;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            button.colors = colors;

            button.onClick.AddListener(ReturnToContainer);

            // Label
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            Text label = labelObject.GetComponent<Text>();
            label.text = backButtonLabel;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = labelFontSize;
            label.color = labelColor;
            label.alignment = TextAnchor.MiddleCenter;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private static void ApplyCorner(RectTransform rect, OverlayButtonCorner corner, Vector2 padding, Vector2 size)
        {
            bool isRight  = corner == OverlayButtonCorner.TopRight  || corner == OverlayButtonCorner.BottomRight;
            bool isBottom = corner == OverlayButtonCorner.BottomLeft || corner == OverlayButtonCorner.BottomRight;

            float anchorX = isRight  ? 1f : 0f;
            float anchorY = isBottom ? 0f : 1f;

            rect.anchorMin = new Vector2(anchorX, anchorY);
            rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot     = new Vector2(anchorX, anchorY);
            rect.sizeDelta = size;

            // Padding pushes inward from the chosen corner.
            float offsetX = isRight  ? -padding.x : padding.x;
            float offsetY = isBottom ?  padding.y : -padding.y;
            rect.anchoredPosition = new Vector2(offsetX, offsetY);
        }
    }
}
