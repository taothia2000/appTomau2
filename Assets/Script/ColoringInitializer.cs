using UnityEngine;

public class ColoringInitializer : MonoBehaviour
{
    [SerializeField] private ColoringManager coloringManager;
    
    private void Start()
    {
        // Tìm ColoringManager nếu chưa được gán
        if (coloringManager == null)
        {
            coloringManager = FindObjectOfType<ColoringManager>();
            if (coloringManager == null)
            {
                Debug.LogError("Không tìm thấy ColoringManager trong scene!");
                return;
            }
        }
        
        // Lấy ID ảnh đã chọn từ Scene trước (ImageListManager)
        string selectedImageId = PlayerPrefs.GetString("SelectedImageId", "");
        
        if (!string.IsNullOrEmpty(selectedImageId))
        {
            // Thiết lập ID ảnh cho ColoringManager
            coloringManager.imageId = selectedImageId;
            
            Debug.Log($"Đã thiết lập ID ảnh: {selectedImageId}");
            
            // Nếu ảnh đã được lưu trước đó, tải lại
            LoadSavedImageIfExists(selectedImageId);
        }
        else
        {
            Debug.LogWarning("Không có ID ảnh nào được chọn trước đó!");
        }
    }
    
    private void LoadSavedImageIfExists(string imageId)
    {
        // Tìm SaveManager
        SaveManager saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            Debug.LogWarning("Không tìm thấy SaveManager trong scene!");
            return;
        }
        
        // Tải ảnh đã lưu
        Texture2D savedTexture = saveManager.LoadSavedImage(imageId);
        
        if (savedTexture != null)
        {
            // TODO: Cần thêm phương thức vào ColoringManager để tải lại ảnh đã lưu
            Debug.Log("Đã tìm thấy ảnh đã lưu, cần thêm phương thức vào ColoringManager để tải lại");
            
            // Đề xuất thêm phương thức này vào ColoringManager:
            // public void LoadFromSavedTexture(Texture2D texture) { ... }
        }
    }
}