using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SafeAreaAnchor : MonoBehaviour
{
    public enum AnchorType
    {
        BottomLeft, BottomCenter, BottomRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        TopLeft, TopCenter, TopRight
    }

    public AnchorType anchorType;
    public Vector3 anchorOffset;
    public bool stretchToSafeArea; // Bật stretch dựa trên kích thước safe area
    public Vector2 stretchRatio = Vector2.one; // Tỷ lệ stretch (0-1) so với kích thước safe area

    private IEnumerator updateAnchorRoutine;

    void Start()
    {
        updateAnchorRoutine = UpdateAnchorAsync();
        StartCoroutine(updateAnchorRoutine);
    }

    IEnumerator UpdateAnchorAsync()
    {
        // Đợi vài frame để đảm bảo Screen.safeArea đã được khởi tạo
        uint waitCycles = 0;
        while (Screen.safeArea.width == 0 || Screen.safeArea.height == 0)
        {
            ++waitCycles;
            yield return new WaitForEndOfFrame();
        }
        if (waitCycles > 0)
        {
            Debug.Log($"SafeAreaAnchor initialized after {waitCycles} frame(s).");
        }
        UpdateAnchor();
        updateAnchorRoutine = null;
    }

void UpdateAnchor()
{
    Vector3 anchorPos = GetAnchorPosition(anchorType);

    if (stretchToSafeArea)
    {
        // Lấy kích thước safe area
        float safeAreaWidth = Screen.safeArea.width;
        float safeAreaHeight = Screen.safeArea.height;

        // Kiểm tra safe area hợp lệ
        if (safeAreaWidth <= 0 || safeAreaHeight <= 0)
        {
            Debug.LogWarning("SafeArea is invalid, skipping update.");
            return;
        }

        // Đảm bảo stretchRatio hợp lệ
        stretchRatio.x = Mathf.Clamp01(stretchRatio.x);
        stretchRatio.y = Mathf.Clamp01(stretchRatio.y);

        // Tính kích thước dựa trên safe area và stretchRatio
        Vector2 size = new Vector2(
            safeAreaWidth * stretchRatio.x,
            safeAreaHeight * stretchRatio.y
        );

        // Điều chỉnh vị trí (giữa khu vực stretch)
        Vector3 newPos = anchorPos + new Vector3(
            size.x * 0.5f * (anchorType == AnchorType.BottomLeft || anchorType == AnchorType.MiddleLeft || anchorType == AnchorType.TopLeft ? 1 : 0),
            size.y * 0.5f * (anchorType == AnchorType.BottomLeft || anchorType == AnchorType.BottomCenter || anchorType == AnchorType.BottomRight ? 1 : 0),
            0f
        ) + anchorOffset;

        // Nếu là UI (có RectTransform), điều chỉnh sizeDelta thay vì localScale
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size; // Đặt kích thước UI bằng sizeDelta
            rectTransform.position = newPos;
        }
        else
        {
            // Nếu không phải UI, giữ localScale hợp lý
            transform.position = newPos;
            // Đặt localScale theo tỷ lệ hợp lý (ví dụ: chia cho một giá trị lớn để đưa về khoảng 0-1)
            Vector3 newScale = new Vector3(size.x / 1000f, size.y / 1000f, transform.localScale.z);
            transform.localScale = newScale;
        }

        Debug.Log($"AnchorPos: {anchorPos}, Size: {size}, NewPos: {newPos}");
    }
    else
    {
        // Không stretch, chỉ đặt vị trí
        Vector3 newPos = anchorPos + anchorOffset;
        if (!transform.position.Equals(newPos))
        {
            transform.position = newPos;
        }
    }
}

    Vector3 GetAnchorPosition(AnchorType anchor)
    {
        Rect safeArea = Screen.safeArea;
        Vector3 safeAreaMin = new Vector3(safeArea.xMin, safeArea.yMin, 0f);
        Vector3 safeAreaMax = new Vector3(safeArea.xMax, safeArea.yMax, 0f);
        Vector3 safeAreaCenter = new Vector3(safeArea.center.x, safeArea.center.y, 0f);

        switch (anchor)
        {
            case AnchorType.BottomLeft: return safeAreaMin;
            case AnchorType.BottomCenter: return new Vector3(safeAreaCenter.x, safeAreaMin.y, 0f);
            case AnchorType.BottomRight: return new Vector3(safeAreaMax.x, safeAreaMin.y, 0f);
            case AnchorType.MiddleLeft: return new Vector3(safeAreaMin.x, safeAreaCenter.y, 0f);
            case AnchorType.MiddleCenter: return safeAreaCenter;
            case AnchorType.MiddleRight: return new Vector3(safeAreaMax.x, safeAreaCenter.y, 0f);
            case AnchorType.TopLeft: return new Vector3(safeAreaMin.x, safeAreaMax.y, 0f);
            case AnchorType.TopCenter: return new Vector3(safeAreaCenter.x, safeAreaMax.y, 0f);
            case AnchorType.TopRight: return safeAreaMax;
            default: return Vector3.zero;
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (updateAnchorRoutine == null)
        {
            updateAnchorRoutine = UpdateAnchorAsync();
            StartCoroutine(updateAnchorRoutine);
        }
    }
#endif
}