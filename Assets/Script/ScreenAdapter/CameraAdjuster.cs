using System.Collections;
using UnityEngine;

public class CameraAdjuster : MonoBehaviour
{
    public float targetAspect = 9f/16f; // Tỷ lệ màn hình dọc (9:16)
    public float orthographicSize = 5f; // Size cơ bản
    
void Start()
{
    Camera cam = GetComponent<Camera>();
    cam.orthographicSize = 5f;
    
    // Đăng ký một event để cập nhật lại kích thước sau khi các script khác thay đổi
    StartCoroutine(EnsureOrthographicSize());
}

 IEnumerator EnsureOrthographicSize()
{
    // Đợi đến cuối frame để các script khác đã xử lý xong
    yield return new WaitForEndOfFrame();
    
    // Đảm bảo orthographicSize = 5 ngay cả khi các script khác thay đổi
    Camera cam = GetComponent<Camera>();
    cam.orthographicSize = 5f;
}
}