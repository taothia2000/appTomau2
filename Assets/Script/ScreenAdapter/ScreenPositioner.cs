using UnityEngine;

public class ScreenPositioner : MonoBehaviour
{
    public enum ScreenAnchor { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight }
    public ScreenAnchor anchorPosition;
    public Vector2 offset;
    public bool adjustScale = true;
    public float scaleMultiplier = 1.0f;

    [SerializeField] private Camera targetCamera;

    private Vector2 lastScreenSize;
    private Vector3 originalScale;
    private CameraAdjuster cameraAdjuster;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("[ScreenPositioner] Không có camera nào được gán và không tìm thấy Main Camera trong scene!");
                return;
            }
            Debug.LogWarning("[ScreenPositioner] Target Camera chưa được gán, sử dụng Main Camera mặc định.");
        }

        cameraAdjuster = targetCamera.GetComponent<CameraAdjuster>();
        if (cameraAdjuster == null)
        {
            Debug.LogWarning("[ScreenPositioner] Không tìm thấy CameraAdjuster trên targetCamera, dùng aspect mặc định.");
        }

        originalScale = transform.localScale;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        PositionObject();
    }

    void Update()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (Vector2.SqrMagnitude(currentScreenSize - lastScreenSize) > 0.01f) // Ngưỡng nhạy hơn
        {
            lastScreenSize = currentScreenSize;
            Debug.Log("[ScreenPositioner] Detected screen size change, calling PositionObject");
            PositionObject();
        }
    }

    void PositionObject()
{
    if (targetCamera == null) return;

    float orthoSize = targetCamera.orthographicSize;
    float cameraHeight = 2f * orthoSize;
    float targetAspect = (cameraAdjuster != null) ? cameraAdjuster.targetAspect : targetCamera.aspect;
    float cameraWidth = cameraHeight * targetAspect;

    Debug.Log($"[ScreenPositioner] Chiều rộng camera={cameraWidth:F2}, Chiều cao camera={cameraHeight:F2}, Tỷ lệ={targetAspect:F2}");

    // Tính tỷ lệ để background khớp với camera
    float widthRatio = cameraWidth / originalScale.x;
    float heightRatio = cameraHeight / originalScale.y;
    float scaleRatio = Mathf.Max(widthRatio, heightRatio);
    scaleRatio = Mathf.Clamp(scaleRatio, 0.1f, 10f); // Giới hạn để tránh quá nhỏ/quá lớn

    Vector3 newScale = new Vector3(scaleRatio, scaleRatio, 1) * scaleMultiplier;

    if (adjustScale)
    {
        transform.localScale = newScale;
    }
    else
    {
        transform.localScale = originalScale;
    }

    // Đặt background vào giữa khung hình
    Vector3 viewportPosition = new Vector3(0.5f, 0.5f, 0);
    Vector3 worldPos = targetCamera.ViewportToWorldPoint(viewportPosition);
    worldPos.z = 0; // Đặt Z = 0 để khớp với background
    worldPos += new Vector3(offset.x, offset.y, 0);
    transform.position = worldPos;
}
}