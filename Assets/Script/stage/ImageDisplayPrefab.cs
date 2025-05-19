using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageDisplayPrefab : MonoBehaviour
{
    public Image displayImage;
    public GameObject completionIndicator;

public void SetupDisplay(string imageId, Texture2D texture, bool isCompleted)
{
    if (texture != null && displayImage != null)
    {
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );

        displayImage.sprite = sprite;
        displayImage.preserveAspect = true;
        
        RectTransform rect = displayImage.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(texture.width, texture.height);
            rect.localScale = Vector3.one;
        }
    }

    if (completionIndicator != null)
    {
        completionIndicator.SetActive(false);
    }
}
}