using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private float targetWidth = 800f;
    [SerializeField] private float targetHeight = 600f;

    private Camera targetCamera;
    private Camera barCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Awake()
    {
        targetCamera = GetComponent<Camera>();
        CreateBarCamera();
    }

    void Start()
    {
        ApplyAspect();
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyAspect();
        }
    }

    // A second camera at depth -2 fills the whole screen with black.
    // The main camera renders on top of it at depth -1, covering only the 4:3 area.
    // The areas outside the 4:3 rect show the black from the bar camera.
    private void CreateBarCamera()
    {
        GameObject barGO = new GameObject("BarCamera");
        barGO.transform.SetParent(transform);
        barCamera = barGO.AddComponent<Camera>();
        barCamera.clearFlags = CameraClearFlags.SolidColor;
        barCamera.backgroundColor = Color.black;
        barCamera.cullingMask = 0; // renders nothing, just clears to black
        barCamera.depth = targetCamera.depth - 1;
        barCamera.rect = new Rect(0f, 0f, 1f, 1f);
        barCamera.orthographic = true;
    }

    private void ApplyAspect()
    {
        if (targetCamera == null)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float safeWidth = Mathf.Max(1f, targetWidth);
        float safeHeight = Mathf.Max(1f, targetHeight);
        float targetAspect = safeWidth / safeHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;

        Rect rect = new Rect();

        if (scale < 1.0f)
        {
            // Screen is taller than 4:3 -> bars on top/bottom.
            rect.width = 1.0f;
            rect.height = scale;
            rect.x = 0f;
            rect.y = (1.0f - scale) / 2.0f;
        }
        else
        {
            // Screen is wider than 4:3 -> bars on left/right.
            float scaleWidth = 1.0f / scale;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0f;
        }

        targetCamera.rect = rect;
    }
}