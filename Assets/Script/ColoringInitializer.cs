using UnityEngine;

public class ColoringInitializer : MonoBehaviour
{
    [SerializeField] private ColoringManager coloringManager;
    
    private void Start()
    {
        if (coloringManager == null)
        {
            coloringManager = FindObjectOfType<ColoringManager>();
            if (coloringManager == null)
            {
                return;
            }
        }
        
        string selectedImageId = PlayerPrefs.GetString("SelectedImageId", "");
        
        if (!string.IsNullOrEmpty(selectedImageId))
        {
            coloringManager.imageId = selectedImageId;
            
            
            LoadSavedImageIfExists(selectedImageId);
        }
    }
    
    private void LoadSavedImageIfExists(string imageId)
    {
        SaveManager saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            return;
        }
        
        // Tải ảnh đã lưu
        Texture2D savedTexture = saveManager.LoadSavedImage(imageId);
        
        if (savedTexture != null)
        {
            Debug.Log("Đã tìm thấy ảnh đã lưu, cần thêm phương thức vào ColoringManager để tải lại");
            
        }
    }
}