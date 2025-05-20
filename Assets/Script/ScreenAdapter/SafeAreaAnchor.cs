using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SafeAreaAnchor : MonoBehaviour
{
    public enum AnchorType
    {
        BottomLeft, BottomCenter, BottomRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        TopLeft, TopCenter, TopRight
    }

    public AnchorType anchorType = AnchorType.BottomCenter; // Mặc định là BottomCenter để giữ dưới cùng
    public Vector3 anchorOffset;
    public bool stretchToSafeArea;
    public Vector2 stretchRatio = Vector2.one;
    [Tooltip("Maximum size for sizeDelta to prevent extreme scaling")]
    public Vector2 maxSizeDelta = new Vector2(2000, 2000);
    [Tooltip("Minimum size for sizeDelta to prevent extreme scaling")]
    public Vector2 minSizeDelta = new Vector2(100, 100);

    private IEnumerator updateAnchorRoutine;
    private Rect lastSafeArea;
    private Vector2 lastScreenSize;
    private RectTransform rectTransform;
    private CanvasScaler canvasScaler;
    private ScrollRect scrollRect;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasScaler = canvas ? canvas.GetComponent<CanvasScaler>() : null;
        scrollRect = GetComponentInParent<ScrollRect>();

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        updateAnchorRoutine = UpdateAnchorAsync();
        StartCoroutine(updateAnchorRoutine);

        Debug.Log($"[Start] Initial Screen.safeArea: {Screen.safeArea}, Screen Size: {Screen.width}x{Screen.height}, AnchorType: {anchorType}");
    }

    void Update()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (!lastSafeArea.Equals(Screen.safeArea) || lastScreenSize != currentScreenSize)
        {
            Debug.Log($"[Update] Screen.safeArea changed to: {Screen.safeArea}, Screen Size changed to: {Screen.width}x{Screen.height}");
            lastSafeArea = Screen.safeArea;
            lastScreenSize = currentScreenSize;
            UpdateAnchor();
        }
    }

    IEnumerator UpdateAnchorAsync()
    {
        uint waitCycles = 0;
        while (Screen.safeArea.width == 0 || Screen.safeArea.height == 0 || Screen.width == 0 || Screen.height == 0)
        {
            Debug.Log($"[UpdateAnchorAsync] Waiting... Frame {waitCycles}, Current Screen.safeArea: {Screen.safeArea}, Screen Size: {Screen.width}x{Screen.height}");
            ++waitCycles;
            yield return new WaitForEndOfFrame();
        }
        if (waitCycles > 0)
        {
            Debug.Log($"[UpdateAnchorAsync] Initialized after {waitCycles} frame(s). Final Screen.safeArea: {Screen.safeArea}, Screen Size: {Screen.width}x{Screen.height}");
        }
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        UpdateAnchor();
        updateAnchorRoutine = null;
    }

    void UpdateAnchor()
    {
        if (Screen.safeArea.width <= 0 || Screen.safeArea.height <= 0 || Screen.width <= 0 || Screen.height <= 0)
        {
            Debug.LogWarning($"[UpdateAnchor] SafeArea or Screen size is invalid: Screen.safeArea: {Screen.safeArea}, Screen Size: {Screen.width}x{Screen.height}");
            return;
        }

        Vector3 anchorPos = GetAnchorPosition(anchorType);
        Debug.Log($"[UpdateAnchor] Calculated AnchorPos: {anchorPos}");

        if (stretchToSafeArea)
        {
            float safeAreaWidth = Screen.safeArea.width;
            float safeAreaHeight = Screen.safeArea.height;

            stretchRatio.x = Mathf.Clamp01(stretchRatio.x);
            stretchRatio.y = Mathf.Clamp01(stretchRatio.y);

            Vector2 size = new Vector2(safeAreaWidth * stretchRatio.x, safeAreaHeight * stretchRatio.y);
            Debug.Log($"[UpdateAnchor] Calculated Size: {size}");

            if (canvasScaler != null && canvasScaler.referenceResolution != Vector2.zero)
            {
                float scaleFactor = Mathf.Min(
                    Screen.width / canvasScaler.referenceResolution.x,
                    Screen.height / canvasScaler.referenceResolution.y
                );
                size = new Vector2(
                    Mathf.Clamp(size.x / scaleFactor, minSizeDelta.x, maxSizeDelta.x),
                    Mathf.Clamp(size.y / scaleFactor, minSizeDelta.y, maxSizeDelta.y)
                );
                Debug.Log($"[UpdateAnchor] Adjusted Size with CanvasScaler (scaleFactor: {scaleFactor}): {size}");
            }
            else
            {
                size = new Vector2(
                    Mathf.Clamp(size.x, minSizeDelta.x, maxSizeDelta.x),
                    Mathf.Clamp(size.y, minSizeDelta.y, maxSizeDelta.y)
                );
                Debug.Log($"[UpdateAnchor] Adjusted Size without CanvasScaler: {size}");
            }

            if (scrollRect != null)
            {
                RectTransform content = scrollRect.content;
                if (content != null)
                {
                    content.anchorMin = new Vector2(0, 0);
                    content.anchorMax = new Vector2(0, 0);
                    content.pivot = new Vector2(0.5f, 0.5f);
                    content.sizeDelta = size;

                    Vector3 newPos = anchorPos + anchorOffset;
                    if (!content.position.Equals(newPos))
                    {
                        content.position = newPos;
                        Debug.Log($"[UpdateAnchor] Applied to ScrollRect Content - NewPos: {newPos}, SizeDelta: {size}");
                    }
                }
            }
            else if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = size;
                Vector3 newPos = anchorPos + anchorOffset;
                if (!rectTransform.position.Equals(newPos))
                {
                    rectTransform.position = newPos;
                    Debug.Log($"[UpdateAnchor] Applied to RectTransform - NewPos: {newPos}, SizeDelta: {size}");
                }
            }
        }
        else
        {
            Vector3 newPos = anchorPos + anchorOffset;
            if (rectTransform != null && !rectTransform.position.Equals(newPos))
            {
                rectTransform.position = newPos;
                Debug.Log($"[UpdateAnchor] Applied without stretch - NewPos: {newPos}");
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
    void OnValidate()
    {
        if (updateAnchorRoutine == null)
        {
            updateAnchorRoutine = UpdateAnchorAsync();
            StartCoroutine(updateAnchorRoutine);
            Debug.Log($"[OnValidate] Triggered UpdateAnchorAsync, Initial Screen.safeArea: {Screen.safeArea}, AnchorType: {anchorType}");
        }
    }
#endif
}