using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class CameraAnchor : MonoBehaviour
{
    public enum AnchorType
    {
        BottomLeft, BottomCenter, BottomRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        TopLeft, TopCenter, TopRight
    }

    public AnchorType anchorType;
    public Vector3 anchorOffset;
    public bool stretchToViewport; // Bật stretch dựa trên kích thước viewport
    public Vector2 stretchRatio = Vector2.one; // Tỷ lệ stretch (0-1) so với kích thước viewport

    private IEnumerator updateAnchorRoutine;

    void Start()
    {
        updateAnchorRoutine = UpdateAnchorAsync();
        StartCoroutine(updateAnchorRoutine);
    }

    IEnumerator UpdateAnchorAsync()
    {
        uint cameraWaitCycles = 0;
        while (ViewportHandler.Instance == null)
        {
            ++cameraWaitCycles;
            yield return new WaitForEndOfFrame();
        }
        if (cameraWaitCycles > 0)
        {
            Debug.Log($"CameraAnchor found ViewportHandler after {cameraWaitCycles} frame(s).");
        }
        UpdateAnchor();
        updateAnchorRoutine = null;
    }

    void UpdateAnchor()
    {
        Vector3 anchorPos = GetAnchorPosition(anchorType);

        if (stretchToViewport)
        {
            // Lấy kích thước viewport từ ViewportHandler
            float viewportWidth = ViewportHandler.Instance.Width;
            float viewportHeight = ViewportHandler.Instance.Height;

            // Tính toán kích thước stretch dựa trên tỷ lệ
            Vector3 size = new Vector3(
                viewportWidth * stretchRatio.x,
                viewportHeight * stretchRatio.y,
                0f
            );

            // Điều chỉnh vị trí (giữa khu vực stretch)
            Vector3 newPos = anchorPos + new Vector3(
                size.x * 0.5f * (anchorType == AnchorType.BottomLeft || anchorType == AnchorType.MiddleLeft || anchorType == AnchorType.TopLeft ? 1 : 0),
                size.y * 0.5f * (anchorType == AnchorType.BottomLeft || anchorType == AnchorType.BottomCenter || anchorType == AnchorType.BottomRight ? 1 : 0),
                0f
            ) + anchorOffset;

            // Điều chỉnh scale dựa trên kích thước
            Vector3 currentScale = transform.localScale;
            Vector3 newScale = new Vector3(
                size.x, // Scale theo chiều rộng viewport
                size.y, // Scale theo chiều cao viewport
                currentScale.z // Giữ nguyên Z
            );

            transform.position = newPos;
            transform.localScale = newScale;
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
        switch (anchor)
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