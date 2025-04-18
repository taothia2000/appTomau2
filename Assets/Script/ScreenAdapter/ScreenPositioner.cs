using UnityEngine;

public class ScreenPositioner : MonoBehaviour
{
    public enum ScreenAnchor { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight }
    public ScreenAnchor anchorPosition;
    public Vector2 offset;
    public bool adjustScale = false;
    public float scaleMultiplier = 0.5f;
    
    private Vector2 lastScreenSize;
    private Vector3 originalScale;
    private Vector2 referenceResolution = new Vector2(1920, 1080); // Độ phân giải tham chiếu

    void Start()
    {
        // Lưu lại scale gốc của object
        originalScale = transform.localScale;
        
        // Lưu kích thước màn hình ban đầu
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        
        // Áp dụng vị trí và scale ngay khi bắt đầu
        PositionObject();
    }

    void Update()
    {
        // Kiểm tra nếu kích thước màn hình thay đổi
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            PositionObject();
        }
    }
    
    void PositionObject()
    {
        Camera cam = Camera.main;
        Vector3 viewportPosition = Vector3.zero;
        
        // Xác định vị trí theo viewport (0-1)
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
        worldPos.z = 0; // Đảm bảo object nằm trên mặt phẳng z=0
        
        // Áp dụng offset
        worldPos += new Vector3(offset.x, offset.y, 0);
        
        // Đặt vị trí object
        transform.position = worldPos;
        
        // Áp dụng scale tự động khi thay đổi khung hình
        if (adjustScale)
        {
            // Tính toán tỉ lệ phóng to dựa trên kích thước màn hình
            float screenRatio;
            Vector3 newScale = originalScale;
            
            if (cam.orthographic)
            {
                // Với ứng dụng portrait, lấy tỉ lệ phóng to để lấp đầy chiều rộng
                float screenWidth = cam.orthographicSize * 2.0f * ((float)Screen.width / Screen.height);
                float screenHeight = cam.orthographicSize * 2.0f;
                
                // Tính tỉ lệ phóng to để lấp đầy hầu hết màn hình
                float widthRatio = screenWidth / 5.0f; // Điều chỉnh số này để phù hợp với kích thước đối tượng gốc
                float heightRatio = screenHeight / 5.0f;
                
                // Lấy giá trị lớn hơn để đảm bảo đối tượng lấp đầy phần lớn màn hình
                screenRatio = Mathf.Max(widthRatio, heightRatio);
                
                // Tính scale mới, đảm bảo lớn hơn scale gốc
                newScale *= screenRatio * scaleMultiplier;
            }
            else
            {
                // Với perspective camera
                float distance = Mathf.Abs(transform.position.z - cam.transform.position.z);
                float viewportHeight = 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float viewportWidth = viewportHeight * cam.aspect;
                
                // Tính tỉ lệ phóng to
                float widthRatio = viewportWidth / 5.0f;
                float heightRatio = viewportHeight / 5.0f;
                
                // Lấy giá trị lớn hơn
                screenRatio = Mathf.Max(widthRatio, heightRatio);
                
                // Tính scale mới
                newScale *= screenRatio * scaleMultiplier;
            }
            
            // Đảm bảo scale không bao giờ nhỏ hơn scale gốc
            newScale.x = Mathf.Max(newScale.x, originalScale.x);
            newScale.y = Mathf.Max(newScale.y, originalScale.y);
            newScale.z = originalScale.z;
            
            // Áp dụng scale mới
            transform.localScale = newScale;
        }
        else
        {
            // Nếu không điều chỉnh scale, sử dụng scale gốc
            transform.localScale = originalScale;
        }
    }
}