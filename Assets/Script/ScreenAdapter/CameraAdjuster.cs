using System.Collections;
using UnityEngine;

public class CameraAdjuster : MonoBehaviour
{
    public float targetAspect = 9f/16f; // Tỷ lệ màn hình dọc (9:16)
    public float baseOrthographicSize = 5f; // Size cơ bản
    private float currentScreenRatio;
    
    void Start()
    {
        AdjustCamera();
    }
    
    void Update()
    {
        // Kiểm tra nếu tỷ lệ màn hình thay đổi
        float screenRatio = (float)Screen.width / Screen.height;
        if (screenRatio != currentScreenRatio)
        {
            currentScreenRatio = screenRatio;
            AdjustCamera();
        }
    }
    
    void AdjustCamera()
    {
        Camera cam = GetComponent<Camera>();
        float screenRatio = (float)Screen.width / Screen.height;
        
        // Điều chỉnh orthographicSize dựa trên tỷ lệ màn hình
        if (screenRatio < targetAspect)
        {
            // Màn hình rộng hơn so với tỷ lệ mục tiêu
            cam.orthographicSize = baseOrthographicSize;
        }
        else
        {
            // Màn hình hẹp hơn so với tỷ lệ mục tiêu
            float differenceInSize = targetAspect / screenRatio;
            cam.orthographicSize = baseOrthographicSize * differenceInSize;
        }
    }
}