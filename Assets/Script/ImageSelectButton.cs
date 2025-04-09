using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ImageSelectButton : MonoBehaviour
{
    public Image thumbnailImage;
    public GameObject completionBadge;
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
    
    public void Setup(string id, Texture2D texture, bool isCompleted, string lastSavedTime)
{
    imageId = id;
    
    if (texture != null && thumbnailImage != null)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        
        thumbnailImage.sprite = sprite;
        thumbnailImage.preserveAspect = true;
    }
    
    if (completionBadge != null)
    {
        completionBadge.SetActive(isCompleted);
    }
    
    if (lastSavedText != null)
    {
        lastSavedText.text = "Last saved: " + lastSavedTime;
    }
    
    // Check if thumbnailImage exists before accessing its RectTransform
    if (thumbnailImage != null)
    {
        RectTransform imageRect = thumbnailImage.GetComponent<RectTransform>();
        if (imageRect != null && texture != null)
        {
            // Ensure image is centered
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            
            // This helps different sized images display well
            imageRect.sizeDelta = new Vector2(
                texture.width / texture.height > 1 ? imageRect.sizeDelta.x : imageRect.sizeDelta.x * texture.width / texture.height,
                texture.height / texture.width > 1 ? imageRect.sizeDelta.y : imageRect.sizeDelta.y * texture.height / texture.width
            );
        }
    }
    
    // Check for Thumbnail child object
    Transform thumbnailTransform = transform.Find("Thumbnail");
    if (thumbnailTransform != null)
    {
        Image buttonImage = thumbnailTransform.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.preserveAspect = true;
        }
    }
}

    public void OnButtonClick()
{
    Debug.Log($"OnButtonClick: Chọn imageId: {imageId}");
    
    // Lưu ID trước
    PlayerPrefs.SetString("SelectedImageId", imageId);
    PlayerPrefs.Save();
    
    StartCoroutine(SaveImageAndLoadScene());
}

private IEnumerator SaveImageAndLoadScene()
{
    // Đảm bảo SaveManager đã được tìm thấy
    if (saveManager == null)
    {
        saveManager = FindObjectOfType<SaveManager>();
    }
    
    if (saveManager != null)
    {
        Texture2D existingTexture = saveManager.LoadSavedImage(imageId);
        
        if (existingTexture == null && thumbnailImage != null && thumbnailImage.sprite != null)
        {
            // Tạo texture từ thumbnail
            Texture2D thumbnailTexture = new Texture2D(
                (int)thumbnailImage.sprite.rect.width,
                (int)thumbnailImage.sprite.rect.height,
                TextureFormat.RGBA32,
                false
            );
            
            thumbnailTexture.SetPixels(
                thumbnailImage.sprite.texture.GetPixels(
                    (int)thumbnailImage.sprite.textureRect.x,
                    (int)thumbnailImage.sprite.textureRect.y,
                    (int)thumbnailImage.sprite.textureRect.width,
                    (int)thumbnailImage.sprite.textureRect.height
                )
            );
            thumbnailTexture.Apply();
            
            // Lưu texture
            saveManager.SaveImage(imageId, thumbnailTexture);
            Debug.Log($"Đã lưu ảnh mới với ID: {imageId}");
            
            // Chờ một frame để đảm bảo quá trình lưu hình ảnh hoàn tất
            yield return null;
        }
    }
    
    // Chuyển scene sau khi đã lưu hình ảnh
    SceneManager.LoadScene("Main");
}
}