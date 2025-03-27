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
    
    // Event khi người dùng nhấp vào
    public System.Action<string> OnItemClicked;
    
    public void Setup(string id, Texture2D texture, string lastSaved, bool isCompleted)
{
    this.imageId = id;
    
    if (texture != null && thumbnail != null)
    {
        // Tạo sprite với pivot point ở giữa
        Sprite thumbnailSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), // Pivot point ở giữa
            100f, // Pixels per unit
            0, // Extrude edges
            SpriteMeshType.FullRect
        );
        
        thumbnail.sprite = thumbnailSprite;
        thumbnail.preserveAspect = true; // Giữ tỷ lệ khung hình
    }
    
    if (titleText != null) titleText.text = $"Ảnh {id}";
    if (lastSavedText != null) lastSavedText.text = $"Lưu lần cuối: {lastSaved}";
    if (completedBadge != null) completedBadge.SetActive(isCompleted);
}
    
    public void OnClick()
    {
        // Kích hoạt event khi nhấp vào
        if (OnItemClicked != null)
        {
            OnItemClicked.Invoke(imageId);
        }
    }
}