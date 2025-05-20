using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class UIAnchor : MonoBehaviour
{
    public enum AnchorType
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        TopLeft,
        TopCenter,
        TopRight,
    }

    public AnchorType anchorType;
    public Vector2 anchorOffset; // Offset trong không gian UI
    public bool stretchHorizontal; // Stretch theo chiều ngang
    public bool stretchVertical; // Stretch theo chiều dọc
    public Vector2 size; // Kích thước nếu không stretch

    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (ViewportHandler.Instance == null || canvas == null)
            return;

        UpdateAnchor();
    }

    void UpdateAnchor()
    {
        // Lấy vị trí anchor từ ViewportHandler
        Vector3 anchorWorldPos = GetAnchorPosition();
        
        // Chuyển đổi vị trí thế giới sang viewport (0-1)
        Vector3 viewportPos = ViewportHandler.Instance.camera.WorldToViewportPoint(anchorWorldPos);
        
        // Chuyển đổi sang không gian Canvas
        Rect canvasRect = canvas.GetComponent<RectTransform>().rect;
        Vector2 canvasPos = new Vector2(
            viewportPos.x * canvasRect.width,
            viewportPos.y * canvasRect.height
        );

        // Áp dụng offset
        canvasPos += anchorOffset;

        // Cập nhật anchor và vị trí
        if (stretchHorizontal || stretchVertical)
        {
            // Nếu stretch, đặt anchor min/max để ôm sát khu vực
            rectTransform.anchorMin = new Vector2(viewportPos.x, viewportPos.y);
            rectTransform.anchorMax = new Vector2(viewportPos.x, viewportPos.y);
            if (stretchHorizontal)
                rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x + size.x / canvasRect.width, rectTransform.anchorMax.y);
            if (stretchVertical)
                rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, rectTransform.anchorMax.y + size.y / canvasRect.height);

            rectTransform.anchoredPosition = anchorOffset;
        }
        else
        {
            // Nếu không stretch, đặt anchor tại một điểm và sử dụng kích thước cố định
            rectTransform.anchorMin = new Vector2(viewportPos.x, viewportPos.y);
            rectTransform.anchorMax = new Vector2(viewportPos.x, viewportPos.y);
            rectTransform.anchoredPosition = anchorOffset;
            rectTransform.sizeDelta = size;
        }
    }

    Vector3 GetAnchorPosition()
    {
        switch (anchorType)
        {
            case AnchorType.BottomLeft: return ViewportHandler.Instance.BottomLeft;
            case AnchorType.BottomCenter: return ViewportHandler.Instance.BottomCenter;
            case AnchorType.BottomRight: return ViewportHandler.Instance.BottomRight;
            case AnchorType.MiddleLeft: return ViewportHandler.Instance.MiddleLeft;
            case AnchorType.MiddleCenter: return ViewportHandler.Instance.MiddleCenter;
            case AnchorType.MiddleRight: return ViewportHandler.Instance.MiddleRight;
            case AnchorType.TopLeft: return ViewportHandler.Instance.TopLeft;
            case AnchorType.TopCenter: return ViewportHandler.Instance.TopCenter;
            case AnchorType.TopRight: return ViewportHandler.Instance.TopRight;
            default: return Vector3.zero;
        }
    }
}