using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tek.Core
{
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private float targetWidth = 800f;
    [SerializeField] private float targetHeight = 600f;
    [SerializeField] private Sprite barSprite;

    private static AspectRatioEnforcer instance;

    private Camera targetCamera;
    private Camera barCamera;
    private Image barImage;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Rect currentRect;
    private bool enforcing;

    /// <summary>Call this when entering an interactive to activate letterboxing.</summary>
    public void EnableEnforcement()
    {
        enforcing = true;
        ApplyAspect();
    }

    /// <summary>Call this when leaving an interactive to restore full-screen cameras.</summary>
    public void DisableEnforcement()
    {
        enforcing = false;
        currentRect = new Rect(0f, 0f, 1f, 1f);
        if (targetCamera != null) targetCamera.rect = currentRect;
        if (barCamera != null) barCamera.gameObject.SetActive(false);
        ApplyToSceneCameras();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // A persistent enforcer already exists from a previous scene load. Destroy
            // this duplicate so only one camera persists across interactive returns.
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        targetCamera = GetComponent<Camera>();
        CreateBarCamera();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Do not apply on startup; call EnableEnforcement() when entering an interactive.
    }

    private void Update()
    {
        if (!enforcing) return;
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyAspect();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply the current rect to every camera in the newly loaded scene.
        // Skip our own DontDestroyOnLoad cameras (targetCamera, barCamera).
        ApplyToSceneCameras(scene);
    }

    private void ApplyToSceneCameras()
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            ApplyToSceneCameras(SceneManager.GetSceneAt(s));
        }
    }

    private void ApplyToSceneCameras(Scene scene)
    {
        if (!scene.isLoaded) return;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);
            for (int j = 0; j < cameras.Length; j++)
            {
                cameras[j].rect = currentRect;
            }
        }
    }

    private void CreateBarCamera()
    {
        // Bar camera renders only the bar layer, behind everything else
        int barLayer = 31;

        GameObject barGO = new GameObject("BarCamera");
        barGO.transform.SetParent(transform);
        barCamera = barGO.AddComponent<Camera>();
        barCamera.clearFlags = CameraClearFlags.SolidColor;
        barCamera.backgroundColor = Color.black;
        barCamera.cullingMask = 1 << barLayer;
        barCamera.depth = targetCamera.depth - 1;
        barCamera.rect = new Rect(0f, 0f, 1f, 1f);
        barCamera.orthographic = true;

        // Canvas rendered by the bar camera
        GameObject canvasGO = new GameObject("BarCanvas");
        canvasGO.transform.SetParent(barGO.transform);
        canvasGO.layer = barLayer;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = barCamera;
        canvas.sortingOrder = 0;

        // Single full-screen image
        GameObject imgGO = new GameObject("BarImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        imgGO.layer = barLayer;
        barImage = imgGO.AddComponent<Image>();
        barImage.sprite = barSprite;
        barImage.color = Color.white;
        barImage.raycastTarget = false;
        RectTransform rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Bar camera starts inactive; enabled by EnableEnforcement()
        barCamera.gameObject.SetActive(false);
    }

    private void ApplyAspect()
    {
        if (targetCamera == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float targetAspect = Mathf.Max(1f, targetWidth) / Mathf.Max(1f, targetHeight);
        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;

        if (scale < 1.0f)
        {
            currentRect = new Rect(0f, (1f - scale) / 2f, 1f, scale);
        }
        else
        {
            float scaleWidth = 1f / scale;
            currentRect = new Rect((1f - scaleWidth) / 2f, 0f, scaleWidth, 1f);
        }

        targetCamera.rect = currentRect;
        if (barCamera != null) barCamera.gameObject.SetActive(true);
        ApplyToSceneCameras();
    }
}
}