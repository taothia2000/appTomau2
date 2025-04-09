using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ImageDisplayPrefab.cs
public class ImageDisplayPrefab : MonoBehaviour
{
    public Image displayImage;
    public GameObject completionIndicator;

public void SetupDisplay(string imageId, Texture2D texture, bool isCompleted)
{
    Debug.Log($"SetupDisplay called with imageId: {imageId}");
    Debug.Log($"Texture is null: {texture == null}");
    Debug.Log($"DisplayImage is null: {displayImage == null}");
    if (texture != null && displayImage != null)
    {
        Debug.Log($"Setting up display for image {imageId}");
        
        // Tạo sprite với cấu hình chuẩn
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), // Center pivot
            100f,
            0,
            SpriteMeshType.FullRect
        );

        displayImage.sprite = sprite;
        displayImage.preserveAspect = true;
        
        // Điều chỉnh kích thước và vị trí
        RectTransform rect = displayImage.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(texture.width, texture.height);
            rect.localScale = Vector3.one;
        }
    }
    else
    {
        Debug.LogError($"Invalid texture or display image for {imageId}");
    }

    if (completionIndicator != null)
    {
        completionIndicator.SetActive(false);
    }
}
}