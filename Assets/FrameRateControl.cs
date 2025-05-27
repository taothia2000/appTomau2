using UnityEngine;

public class FrameRateControl : MonoBehaviour
{
    void Start()
    {
        QualitySettings.vSyncCount = 0; // Tắt VSync
        Application.targetFrameRate = 144; // Đặt FPS mục tiêu là 60
    }
}