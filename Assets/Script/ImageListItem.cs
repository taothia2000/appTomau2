using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageListItem : MonoBehaviour
{
    public Image thumbnail;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI lastSavedText;
    public GameObject completedBadge;
    
    private string imageId;
    
    public System.Action<string> OnItemClicked;
    
    public void Setup(string id, Texture2D texture, string lastSaved, bool isCompleted)
{
    this.imageId = id;
    
    if (texture != null && thumbnail != null)
    {
        Sprite thumbnailSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        
        thumbnail.sprite = thumbnailSprite;
        thumbnail.preserveAspect = true; 
    }
    
    if (titleText != null) titleText.text = $"Ảnh {id}";
    if (lastSavedText != null) lastSavedText.text = $"Lưu lần cuối: {lastSaved}";
    if (completedBadge != null) completedBadge.SetActive(isCompleted);
}
    
    public void OnClick()
    {
        if (OnItemClicked != null)
        {
            OnItemClicked.Invoke(imageId);
        }
    }
}