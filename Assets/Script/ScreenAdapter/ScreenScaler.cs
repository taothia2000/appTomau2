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
    
    private void Awake()
    {
        mainCamera = Camera.main;
        SetWindowSize(fullscreen, 1920, 1080);
        SetScale();
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
    }
    
    public void SetScale()
    {
        // Tính toán tỷ lệ dựa trên chiều cao màn hình
        scale = baseScaleFactor * windowHeight;
        
        if (vertical)
        {
            scale = (7f / 1200f) * windowHeight;
        }
        
        
        // Áp dụng tỷ lệ cho Canvas nếu cần
        CanvasScaler[] canvasScalers = FindObjectsOfType<CanvasScaler>();
        foreach (CanvasScaler cs in canvasScalers)
        {
            cs.scaleFactor = scale;
        }
    }
    
    public void CheckWindowSize()
    {
        if (Screen.width != windowWidth || Screen.height != windowHeight)
        {
            ReinitSize();
        }
    }
    
    public void ReinitSize()
    {
        windowWidth = Screen.width;
        windowHeight = Screen.height;
        SetScale();
        
        // Ở đây bạn có thể gọi các hàm cần khởi tạo lại khi kích thước thay đổi
        // Ví dụ: ReinitUI(), UpdateFonts(), v.v.
    }
}