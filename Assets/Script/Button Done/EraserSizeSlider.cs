using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu bạn muốn hiển thị giá trị kích thước

public class EraserSizeSlider : MonoBehaviour
{
    [Header("References")]
    public ColoringManager coloringManager; // Tham chiếu đến ColoringManager
    public Slider sizeSlider; // Tham chiếu đến UI Slider
    public TextMeshProUGUI valueText; // Tùy chọn: hiển thị giá trị

    [Header("Settings")]
    public float minSize = 1f;
    public float maxSize = 100f;
    public bool updateLiveWhileDragging = true;

    private void Start()
    {
        // Tự động tìm ColoringManager nếu chưa được gán
        if (coloringManager == null)
        {
            coloringManager = FindObjectOfType<ColoringManager>();
            if (coloringManager == null)
            {
                Debug.LogError("Không tìm thấy ColoringManager!");
                return;
            }
        }

        // Tự động tìm slider trên GameObject này nếu chưa được gán
        if (sizeSlider == null)
        {
            sizeSlider = GetComponent<Slider>();
            if (sizeSlider == null)
            {
                Debug.LogError("Không tìm thấy Slider component!");
                return;
            }
        }

        // Thiết lập giá trị ban đầu cho slider
        sizeSlider.minValue = minSize;
        sizeSlider.maxValue = maxSize;
        sizeSlider.value = coloringManager.eraserSize;
        
        // Đăng ký sự kiện
        sizeSlider.onValueChanged.AddListener(OnSliderValueChanged);
        
        // Cập nhật text hiển thị ban đầu nếu có
        UpdateValueText(sizeSlider.value);
    }

    private void OnSliderValueChanged(float value)
    {
        // Chỉ cập nhật khi đang kéo nếu updateLiveWhileDragging = true
        if (updateLiveWhileDragging)
        {
            UpdateEraserSize(value);
            UpdateValueText(value);
        }
    }

    // Được gọi khi thả slider (khi muốn cập nhật sau khi kéo xong)
    public void OnSliderReleased()
    {
        if (!updateLiveWhileDragging)
        {
            UpdateEraserSize(sizeSlider.value);
            UpdateValueText(sizeSlider.value);
        }
    }

    // Cập nhật kích thước cục tẩy trong ColoringManager
    private void UpdateEraserSize(float size)
    {
        if (coloringManager != null)
        {
            coloringManager.eraserSize = size;
            
            // Nếu đang trong chế độ tẩy, cập nhật kích thước hiện tại
            if (coloringManager.currentMode == ColoringManager.DrawingMode.Erase)
            {
                coloringManager.SetBrushSize(size);
            }
        }
    }

    // Cập nhật text hiển thị giá trị nếu có
    private void UpdateValueText(float value)
    {
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(value).ToString();
        }
    }
}