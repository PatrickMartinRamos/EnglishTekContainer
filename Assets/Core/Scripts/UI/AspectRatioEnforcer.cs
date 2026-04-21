using UnityEngine;

namespace EnglishTek.Core
{
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [SerializeField] private float targetWidth = 800f;
    [SerializeField] private float targetHeight = 600f;

    private Camera targetCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        CreateBarCamera();
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
            rect.width = 1.0f;
            rect.height = scale;
            rect.x = 0f;
            rect.y = (1.0f - scale) / 2.0f;
        }
        else
        {
            float scaleWidth = 1.0f / scale;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0f;
        }

        targetCamera.rect = rect;
    }
}
}