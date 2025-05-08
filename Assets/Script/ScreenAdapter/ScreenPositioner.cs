using UnityEngine;

public class ScreenPositioner : MonoBehaviour
{
    public enum ScreenAnchor { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight }
    public ScreenAnchor anchorPosition;
    public Vector2 offset;
    public bool adjustScale = true; // Đảm bảo luôn bật
    public float scaleMultiplier = 0.5f; // Tăng lên để hiệu ứng rõ ràng hơn
    
    private Vector2 lastScreenSize;
    private Vector3 originalScale;
    private float baseScreenHeight = 1080f; // Màn hình tham chiếu

    void Start()
    {
        originalScale = transform.localScale;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        PositionObject();
    }

    void Update()
    {
        // Cải thiện kiểm tra thay đổi kích thước màn hình
        if (Mathf.Abs(Screen.width - lastScreenSize.x) > 0.1f || 
            Mathf.Abs(Screen.height - lastScreenSize.y) > 0.1f)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            PositionObject();
        }
    }
    
    void PositionObject()
    {
        Camera cam = Camera.main;
        Vector3 viewportPosition = Vector3.zero;
        
        // Xác định vị trí viewport như cũ
        switch (anchorPosition)
        {
            case ScreenAnchor.TopLeft:      viewportPosition = new Vector3(0, 1, 0); break;
            case ScreenAnchor.Top:          viewportPosition = new Vector3(0.5f, 1, 0); break;
            case ScreenAnchor.TopRight:     viewportPosition = new Vector3(1, 1, 0); break;
            case ScreenAnchor.Left:         viewportPosition = new Vector3(0, 0.5f, 0); break;
            case ScreenAnchor.Center:       viewportPosition = new Vector3(0.5f, 0.5f, 0); break;
            case ScreenAnchor.Right:        viewportPosition = new Vector3(1, 0.5f, 0); break;
            case ScreenAnchor.BottomLeft:   viewportPosition = new Vector3(0, 0, 0); break;
            case ScreenAnchor.Bottom:       viewportPosition = new Vector3(0.5f, 0, 0); break;
            case ScreenAnchor.BottomRight:  viewportPosition = new Vector3(1, 0, 0); break;
        }
        
        // Chuyển đổi từ viewport sang world position
        Vector3 worldPos = cam.ViewportToWorldPoint(viewportPosition);
        worldPos.z = 0;
        worldPos += new Vector3(offset.x, offset.y, 0);
        transform.position = worldPos;
        
          if (adjustScale)
    {
        // Tính tỷ lệ màn hình dựa trên chiều rộng thay vì chiều cao
        float widthRatio = (float)Screen.width / 1080f; // 1080 là chiều rộng tham chiếu
        float heightRatio = (float)Screen.height / 1920f; // 1920 là chiều cao tham chiếu
        
        // Sử dụng tỷ lệ nhỏ hơn để đảm bảo đối tượng hiển thị đầy đủ
        float screenRatio = Mathf.Min(widthRatio, heightRatio);
        
        // Áp dụng tỷ lệ thay đổi rõ rệt hơn
        Vector3 newScale;
        if (Screen.width >= 1500) // Màn hình rộng
        {
            newScale = originalScale * 1.5f; // Tăng 50% cho màn hình lớn
        }
        else if (Screen.width <= 1000) // Màn hình nhỏ 
        {
            newScale = originalScale * 0.8f; // Giảm 20% cho màn hình nhỏ
        }
        else // Màn hình trung bình
        {
            newScale = originalScale * 1.0f; // Giữ nguyên scale cho màn hình chuẩn
        }
        
        // Áp dụng scaleMultiplier từ Inspector
        newScale *= scaleMultiplier;
        
        transform.localScale = newScale;
        
        Debug.Log($"Screen size: {Screen.width}x{Screen.height}, Width Ratio: {widthRatio}, " +
                 $"Height Ratio: {heightRatio}, Final Scale: {newScale}");
    }
    else
    {
        transform.localScale = originalScale;
    }
    }
}