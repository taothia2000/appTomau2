using UnityEngine;
using UnityEngine.UI;

public class ScreenScaler : MonoBehaviour
{
    [SerializeField] private bool fullscreen = true;
    [SerializeField] private bool vertical = false;
    [SerializeField] private bool resizable = true;
    [SerializeField] private float baseScaleFactor = 7.3f / 1200f;

    private int windowWidth;
    private int windowHeight;
    private float scale;
    private Camera mainCamera;

    private Vector2 lastResolution;

    private void Awake()
    {
        mainCamera = Camera.main;
        SetWindowSize(fullscreen, 1920, 1080);
        SetScale();
        lastResolution = new Vector2(windowWidth, windowHeight);
    }

    private void Update()
    {
        CheckWindowSize();
    }

    public void SetWindowSize(bool full, int width, int height)
    {
        if (full)
        {
            Screen.fullScreen = true;
            windowWidth = Screen.width;
            windowHeight = Screen.height;
        }
        else
        {
            Screen.fullScreen = false;

            if (width == 0 || height == 0)
            {
                windowWidth = 1920;
                windowHeight = 1080;
            }
            else
            {
                windowWidth = width;
                windowHeight = height;
            }

            Screen.SetResolution(windowWidth, windowHeight, false);
        }

        Debug.Log($"[ScreenScaler] SetWindowSize - Khung hình: {windowWidth}x{windowHeight}");
    }

    public void SetScale()
    {
        CanvasScaler[] canvasScalers = FindObjectsOfType<CanvasScaler>();
        foreach (CanvasScaler cs in canvasScalers)
        {
            if (cs.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                // Giá trị ban đầu làm tham chiếu
                float baseX = 500f; // Giá trị X mặc định
                float baseY = 480f; // Giá trị Y mặc định
                float baseAspectRatio = baseX / baseY; // Tỷ lệ tham chiếu 500:480

                // Tính tỷ lệ dựa trên chiều rộng và chiều cao
                float widthRatio = (float)windowWidth / baseX;
                float heightRatio = (float)windowHeight / baseY;

                // Chọn tỷ lệ nhỏ nhất để tránh UI bị cắt (giống Match Width Or Height)
                float scaleRatio = Mathf.Min(widthRatio, heightRatio * baseAspectRatio);

                // Tính X và Y mới dựa trên tỷ lệ
                float newX = baseX * scaleRatio;
                float newY = baseY * scaleRatio;

                // Debug giá trị X và Y trước khi cập nhật
                Debug.Log($"[ScreenScaler] SetScale - Trước khi cập nhật: X={cs.referenceResolution.x}, Y={cs.referenceResolution.y}");

                // Cập nhật referenceResolution
                cs.referenceResolution = new Vector2(newX, newY);

                // Debug giá trị X và Y sau khi cập nhật
                Debug.Log($"[ScreenScaler] SetScale - Sau khi cập nhật: X={newX}, Y={newY}, Khung hình: {windowWidth}x{windowHeight}");
            }
            else
            {
                // Logic cũ cho Constant Pixel Size
                scale = baseScaleFactor * windowHeight;
                if (vertical)
                {
                    scale = (7f / 1200f) * windowHeight;
                }
                cs.scaleFactor = scale;

                Debug.Log($"[ScreenScaler] SetScale - Constant Pixel Size: Scale Factor={scale}, Khung hình: {windowWidth}x{windowHeight}");
            }
        }
    }

    public void CheckWindowSize()
    {
        if (Screen.width != windowWidth || Screen.height != windowHeight)
        {
            Debug.Log($"[ScreenScaler] CheckWindowSize - Phát hiện thay đổi khung hình: {Screen.width}x{Screen.height}");
            ReinitSize();
        }
    }

    public void ReinitSize()
    {
        windowWidth = Screen.width;
        windowHeight = Screen.height;

        Debug.Log($"[ScreenScaler] ReinitSize - Khung hình mới: {windowWidth}x{windowHeight}");

        SetScale();

        SafeArea[] safeAreas = FindObjectsOfType<SafeArea>();
        foreach (SafeArea safeArea in safeAreas)
        {
            safeArea.AdjustSafeArea();
        }
    }
}