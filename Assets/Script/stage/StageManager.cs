using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    public GameObject imageButtonPrefab;
    public Transform buttonContainer;
    public SaveManager saveManager;
    
    // Thêm cấu hình cho grid layout
    public float buttonSpacing = 10f;
    public int buttonsPerRow = 3;
    public Vector2 buttonSize = new Vector2(200, 200);

    void Start()
    {
        // Find or create SaveManager
        saveManager = SaveManager.Instance;
        if (saveManager == null)
        {
            Debug.LogError("SaveManager not found!");
            return;
        }

        // Register for updates
        saveManager.OnProgressUpdated += HandleProgressUpdate;
        
        // Clear PlayerPrefs on stage screen to avoid stale data
        PlayerPrefs.DeleteKey("SelectedImageId");
        
        // Immediately refresh the list
        RefreshImageList();
        GridLayoutGroup gridLayout = buttonContainer.GetComponent<GridLayoutGroup>();
    if (gridLayout == null)
    {
        gridLayout = buttonContainer.gameObject.AddComponent<GridLayoutGroup>();
    }
    
    // Configure grid layout
    gridLayout.cellSize = buttonSize;
    gridLayout.spacing = new Vector2(buttonSpacing, buttonSpacing); 
    gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
    gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
    gridLayout.childAlignment = TextAnchor.UpperLeft;
    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    gridLayout.constraintCount = buttonsPerRow;
    }
    
    void Awake()
    {
        // Load prefab if not assigned
        if (imageButtonPrefab == null)
        {
            imageButtonPrefab = Resources.Load<GameObject>("Prefabs/ImageSelectButton");
            if (imageButtonPrefab == null)
            {
                Debug.LogError("ImageSelectButton prefab not found in Resources/Prefabs!");
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
    // Clear existing buttons
    foreach (Transform child in buttonContainer)
    {
        Destroy(child.gameObject);
    }
    
    // Get fresh progress data
    var progressList = saveManager.GetAllProgress();
    Debug.Log($"Found {progressList.Count} saved images to display");
    
    foreach (var progress in progressList)
    {
        try 
        {
            // Kiểm tra prefab trước khi khởi tạo
            if (imageButtonPrefab == null)
            {
                Debug.LogError("Image button prefab is null!");
                continue;
            }
            
            GameObject buttonObj = Instantiate(imageButtonPrefab, buttonContainer);

            Image bgImage = buttonObj.AddComponent<Image>();
            bgImage.color = new Color(
            Random.value, // Random red
            Random.value, // Random green
            Random.value, // Random blue
            0.5f         // Semi-transparent
            );
            buttonObj.name = $"ImageButton_{progress.imageId}";
            
            // Kiểm tra component trên button
            ImageSelectButton buttonScript = buttonObj.GetComponent<ImageSelectButton>();
            
            if (buttonScript == null)
            {
                Debug.LogError($"ImageSelectButton component missing on prefab for {progress.imageId}");
                
                // Tạo component mới nếu cần
                buttonScript = buttonObj.AddComponent<ImageSelectButton>();
            }
            
            // Load texture from ID
Texture2D texture = saveManager.LoadSavedImage(progress.imageId);
if (texture != null)
{
    // Setup the button
    buttonScript.Setup(
        progress.imageId, 
        texture, 
        progress.isCompleted,
        progress.lastSaved.ToString("dd/MM/yyyy HH:mm")
    );

    // Add click handler
    Button button = buttonObj.GetComponent<Button>();
    if (button != null)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            buttonScript.OnButtonClick();
        });
    }
    
    Debug.Log($"Successfully created button for image: {progress.imageId}");
}
else
{
    Debug.LogWarning($"Couldn't load texture for image ID: {progress.imageId}");
    Destroy(buttonObj);
}
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating image button: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
}
    
    // Phương thức load ảnh dựa trên ID
    public void LoadImageById(string imageId)
    {
        // Lưu ID đã chọn
        PlayerPrefs.SetString("SelectedImageId", imageId);
        PlayerPrefs.Save();
        
        // Load scene màn hình vẽ (tùy theo project của bạn)
        UnityEngine.SceneManagement.SceneManager.LoadScene("ColoringScene");
    }
}