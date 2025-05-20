using System.Collections;
using UnityEngine;

public class CameraAdjuster : MonoBehaviour
{
    public float targetAspect = 0.9f; // Tỷ lệ khung hình mục tiêu (Inspector đang đặt là 0.9)
    public float baseOrthographicSize = 5f; // Kích thước camera ban đầu (Inspector cũng đang là 5)
    private float currentScreenRatio; // Tỷ lệ màn hình hiện tại (tính bằng chiều rộng / chiều cao)

    void Start()
    {
        AdjustCamera(); // Gọi hàm điều chỉnh camera ngay khi bắt đầu
    }

    void Update()
    {
        float screenRatio = (float)Screen.width / Screen.height; // Tính tỷ lệ màn hình hiện tại
        if (Mathf.Abs(screenRatio - currentScreenRatio) > 0.01f) // Nếu tỷ lệ màn hình thay đổi nhiều
        {
            currentScreenRatio = screenRatio;
            AdjustCamera(); // Gọi lại hàm điều chỉnh camera
        }
    }

    void AdjustCamera()
    {
        Camera cam = GetComponent<Camera>(); // Lấy camera gắn với script này
        float screenRatio = (float)Screen.width / Screen.height; // Tính tỷ lệ màn hình

        Debug.Log($"[CameraAdjuster] Tỷ lệ màn hình={screenRatio:F2}, Tỷ lệ mục tiêu={targetAspect:F2}, Kích thước camera trước={cam.orthographicSize:F2}");

        if (screenRatio < targetAspect) // Nếu màn hình hẹp hơn tỷ lệ mục tiêu
        {
            cam.orthographicSize = baseOrthographicSize; // Giữ nguyên kích thước camera
        }
        else // Nếu màn hình rộng hơn tỷ lệ mục tiêu
        {
            float differenceInSize = targetAspect / screenRatio; // Tính tỷ lệ điều chỉnh
            cam.orthographicSize = baseOrthographicSize * differenceInSize; // Thu nhỏ kích thước camera
        }

        Debug.Log($"[CameraAdjuster] Kích thước camera sau={cam.orthographicSize:F2}");
    }
}