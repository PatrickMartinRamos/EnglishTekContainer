using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tek.Core
{
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private float targetWidth = 800f;
    [SerializeField] private float targetHeight = 600f;

    private Camera targetCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Rect currentRect;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        targetCamera = GetComponent<Camera>();
        CreateBarCamera();
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
        ApplyAspect();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyAspect();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply the current letterbox rect to every camera in the newly loaded scene only.
        // We intentionally skip our own cameras (targetCamera, bar camera) which are in
        // DontDestroyOnLoad and are not part of the loaded scene.
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
        GameObject barGO = new GameObject("BarCamera");
        barGO.transform.SetParent(transform);
        Camera barCamera = barGO.AddComponent<Camera>();
        barCamera.clearFlags = CameraClearFlags.SolidColor;
        barCamera.backgroundColor = Color.black;
        barCamera.cullingMask = 0;
        barCamera.depth = targetCamera.depth - 1;
        barCamera.rect = new Rect(0f, 0f, 1f, 1f);
        barCamera.orthographic = true;
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
    }
}
}