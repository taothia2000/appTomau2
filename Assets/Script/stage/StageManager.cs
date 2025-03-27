using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public  GameObject imagePrefab;
    public Transform imageContainer;
    public SaveManager saveManager;

    void Start()
    {
       saveManager = FindObjectOfType<SaveManager>();
       if (saveManager == null)
        {
            Debug.LogError("Không tìm thấy SaveManager!");
            return;
        }

        saveManager.OnProgressUpdated += HandleProgressUpdate;
        RefreshImageList();
    }
    void Awake()
{
    // Load prefab nếu chưa được gán
    if (imagePrefab == null)
    {
        imagePrefab = Resources.Load<GameObject>("Prefabs/ImageDisplayPrefab");
        if (imagePrefab == null)
        {
            Debug.LogError("ImageDisplayPrefab không tìm thấy trong Resources/Prefabs!");
        }
    }
}

    void OnDestroy()
    {
        if (saveManager != null)
        {
            saveManager.OnProgressUpdated -= HandleProgressUpdate;
        }
    }
private void HandleProgressUpdate(List<SaveManager.ColoringProgress> progress)
    {
        RefreshImageList();
    }

   private void RefreshImageList()
{
    if (!imageContainer)
    {
        Debug.LogError("Image Container không được gán!");
        return;
    }

    // Xóa danh sách cũ
    foreach (Transform child in imageContainer)
    {
        Destroy(child.gameObject);
    }

    var progressList = saveManager.GetAllProgress();
    
    foreach (var progress in progressList)
    {
        try 
        {
            GameObject imageObj = Instantiate(imagePrefab, imageContainer);
            imageObj.transform.localScale = Vector3.one;
            
            // Đặt vị trí
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
            }

            ImageDisplayPrefab display = imageObj.GetComponent<ImageDisplayPrefab>();
            if (display != null)
            {
                Texture2D texture = saveManager.LoadSavedImage(progress.imageId);
                if (texture != null)
                {
                    display.SetupDisplay(progress.imageId, texture, progress.isCompleted);
                }
                else
                {
                    Debug.LogError($"Không thể load texture cho {progress.imageId}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi khi tạo image display: {e.Message}");
        }
    }
}
}