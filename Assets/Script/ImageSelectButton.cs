using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class ImageSelectButton : MonoBehaviour
{
    public Image thumbnailImage;
    // Loại bỏ completionBadge
    public Text lastSavedText;

    public string imageId;
    private SaveManager saveManager;
    
    private void Start()
    {
        // Tìm SaveManager
        saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            Debug.LogWarning("Không tìm thấy SaveManager");
            saveManager = SaveManager.Instance;
        }
    }
    
   public void Setup(string id, Texture2D texture, string lastSavedTime)
{
    imageId = id;
    
    if (thumbnailImage == null)
    {
        Debug.LogError($"Không tìm thấy thumbnailImage cho button {id}");
        return;
    }
    
    if (texture != null)
    {
        // Xóa sprite cũ nếu có
        if (thumbnailImage.sprite != null)
        {
            Destroy(thumbnailImage.sprite);
        }
        
        // Tạo sprite mới
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        
        thumbnailImage.sprite = sprite;
        thumbnailImage.preserveAspect = true;
    }
    
    if (lastSavedText != null)
    {
        lastSavedText.text = "Lưu lần cuối: " + lastSavedTime;
    }
    
    gameObject.name = "ImageButton_" + id;
}

    public void OnButtonClick()
    {
        Debug.Log($"OnButtonClick: Chọn imageId: {imageId}");
        
        // Lưu ID trước
        PlayerPrefs.SetString("SelectedImageId", imageId);
        PlayerPrefs.Save();
        
        // Giải phóng bộ nhớ trước khi chuyển scene
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        
        StartCoroutine(SaveImageAndLoadScene());
    }

    private IEnumerator SaveImageAndLoadScene()
    {
        // Ensure SaveManager exists
        if (saveManager == null)
        {
            saveManager = FindObjectOfType<SaveManager>();
        }
        
        if (saveManager != null)
        {
            Texture2D existingTexture = saveManager.LoadSavedImage(imageId);
            
            if (existingTexture == null && thumbnailImage != null && thumbnailImage.sprite != null)
            {
                try {
                    // Create texture using a different approach to reduce memory usage
                    Texture2D thumbnailTexture = null;
                    try {
                        thumbnailTexture = new Texture2D(
                            (int)thumbnailImage.sprite.rect.width,
                            (int)thumbnailImage.sprite.rect.height,
                            TextureFormat.RGBA32,
                            false
                        );
                        
                        // Get pixels directly from sprite to avoid creating intermediate textures
                        Color[] pixels = thumbnailImage.sprite.texture.GetPixels(
                            (int)thumbnailImage.sprite.textureRect.x,
                            (int)thumbnailImage.sprite.textureRect.y,
                            (int)thumbnailImage.sprite.textureRect.width,
                            (int)thumbnailImage.sprite.textureRect.height
                        );
                        
                        thumbnailTexture.SetPixels(pixels);
                        thumbnailTexture.Apply();
                        
                        // Save image directly through SaveManager
                        saveManager.SaveImage(imageId, thumbnailTexture);
                        Debug.Log($"Saved new image with ID: {imageId}");
                    }
                    finally {
                        // Destroy the temporary texture to free memory
                        if (thumbnailTexture != null) {
                            Destroy(thumbnailTexture);
                        }
                    }
                }
                catch (Exception e) {
                    Debug.LogError($"Error saving thumbnail: {e.Message}");
                }
            }
        }
        
        // Make sure to yield return at all code paths
        yield return null;
        
        // Load scene after saving image
        SceneManager.LoadScene("Main");
    }
    
    // Loại bỏ thuộc tính IsCompleted
    
    public string LastSavedTime
    {
        get { return lastSavedText != null ? lastSavedText.text.Replace("Last saved: ", "") : ""; }
    }
    private void OnEnable()
{
    // Kiểm tra xem thumbnailImage có được gán đúng không
    if (thumbnailImage == null)
    {
        Debug.LogError($"thumbnailImage is null in button {gameObject.name}");
        // Tìm lại trong các thành phần con
        thumbnailImage = GetComponentInChildren<Image>();
        if (thumbnailImage != null)
        {
            Debug.Log($"Found thumbnailImage in children for {gameObject.name}");
        }
    }
    else
    {
        if (thumbnailImage.sprite == null)
        {
            Debug.LogWarning($"thumbnailImage has no sprite in button {gameObject.name}");
        }
        else
        {
            Debug.Log($"Button {gameObject.name} has thumbnailImage with sprite size: {thumbnailImage.sprite.rect.width}x{thumbnailImage.sprite.rect.height}");
        }
    }
}
}