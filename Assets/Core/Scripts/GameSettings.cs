using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [Header("Performance Settings")]
    [Tooltip("Target frame rate for the application. Set to -1 for unlimited.")]
    [SerializeField] private int targetFrameRate = 60;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0; // Disable vSync to allow targetFrameRate to take effect
        //Debug.Log($"[PerfSettings] Target Frame Rate set to: {targetFrameRate}");
    }
}
