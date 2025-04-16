using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    public GameObject imageButtonPrefab;
    
    // Replace single container with array of containers
    public Transform[] buttonContainers;
    public SaveManager saveManager;
    
    // Settings for each container
    [System.Serializable]
    public class ContainerSettings
    {
        public Transform container;
        public float buttonSpacing = 10f;
        public int buttonsPerRow = 3;
        public Vector2 buttonSize = new Vector2(100, 100);
        public bool showTemplatesOnly = false;
        public bool showProgressOnly = false;
    }
    
    public ContainerSettings[] containerSettings;
    
    private List<ImageSelectButton> activeButtons = new List<ImageSelectButton>();
    private bool isRefreshing = false;

    private Dictionary<string, ImageSelectButton> buttonCache = new Dictionary<string, ImageSelectButton>();
    private bool isInitialized = false;
    

    void Start()
    {
        Debug.Log("StageManager Start() called");
        StartCoroutine(InitializeManager());
        saveManager.CleanupInvalidProgressEntries();
    }

    private IEnumerator InitializeManager()
{
    Debug.Log("InitializeManager started");
    if (isInitialized) yield break;
    
    for (int i = 0; i < containerSettings.Length; i++)
    {
        if (containerSettings[i].container == null && 
            i < buttonContainers.Length && 
            buttonContainers[i] != null)
        {
            containerSettings[i].container = buttonContainers[i];
        }
    }

    InitializeSaveManager();
    ConfigureGridLayouts();
    
    // Hiển thị tất cả các button, không lọc theo ID
    yield return StartCoroutine(RefreshImageListCoroutine());
        
        isInitialized = true;
        Debug.Log("InitializeManager completed");
    }

    private void InitializeSaveManager()
    {
        Debug.Log("Initializing SaveManager");
        saveManager = SaveManager.Instance;
        if (saveManager == null)
        {
            Debug.LogError("SaveManager not found!");
            return;
        }
        saveManager.OnProgressUpdated += HandleProgressUpdate;
        PlayerPrefs.DeleteKey("SelectedImageId");
        
        // Debug để liệt kê tất cả các tiến độ có sẵn
        var allProgress = saveManager.GetAllProgress();
        Debug.Log($"Total progress entries found: {allProgress.Count}");
        foreach (var progress in allProgress)
        {
            Debug.Log($"ID: {progress.imageId}, Path: {progress.savedPath}, LastSaved: {progress.lastSaved}");
        }
    }

    private void ConfigureGridLayouts()
    {
        Debug.Log("Configuring grid layouts");
        
        // Check if we're using the array or need to convert from the old single container
        if (containerSettings == null || containerSettings.Length == 0)
        {
            // Create default settings based on the single container if available
            if (buttonContainers != null && buttonContainers.Length > 0)
            {
                containerSettings = new ContainerSettings[buttonContainers.Length];
                for (int i = 0; i < buttonContainers.Length; i++)
                {
                    containerSettings[i] = new ContainerSettings
                    {
                        container = buttonContainers[i],
                        buttonSpacing = 10f,
                        buttonsPerRow = 3,
                        buttonSize = new Vector2(100, 100)
                    };
                }
            }
            else
            {
                Debug.LogError("No button containers assigned!");
                return;
            }
        }
        
        // Configure each container
        foreach (var settings in containerSettings)
        {
            if (settings.container == null) continue;
            
            GridLayoutGroup gridLayout = settings.container.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = settings.container.gameObject.AddComponent<GridLayoutGroup>();
            }
            
            gridLayout.cellSize = settings.buttonSize;
            gridLayout.spacing = new Vector2(settings.buttonSpacing, settings.buttonSpacing);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = settings.buttonsPerRow;
        }
        foreach (var settings in containerSettings)
    {
        if (settings.container != null)
        {
            Debug.Log($"Configured container: {settings.container.name}, buttons per row: {settings.buttonsPerRow}");
        }
        else
        {
            Debug.LogWarning("Found null container in settings!");
        }
    }
    }
    
    void Awake()
    {
        Debug.Log("StageManager Awake() called");
        LoadImageButtonPrefab();
    }

    private void LoadImageButtonPrefab()
    {
        if (imageButtonPrefab == null)
        {
            Debug.Log("Loading image button prefab from Resources");
            
            // Đường dẫn chính xác đến prefab
            imageButtonPrefab = Resources.Load<GameObject>("Prefabs/ImageSelectButton");
            
            if (imageButtonPrefab == null)
            {
                Debug.LogError("ImageSelectButton prefab not found in Resources/Prefabs!");
                // Thử tìm các prefab khác có tên tương tự
                GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
                foreach (var prefab in allPrefabs) {
                    Debug.Log($"Found prefab: {prefab.name}");
                }
            }
            else
            {
                Debug.Log("ImageSelectButton prefab loaded successfully");
                
                // Kiểm tra các components
                ImageSelectButton testComponent = imageButtonPrefab.GetComponent<ImageSelectButton>();
                if (testComponent == null) {
                    Debug.LogError("Prefab doesn't have ImageSelectButton component!");
                }
                
                Image testImage = imageButtonPrefab.GetComponentInChildren<Image>();
                if (testImage == null) {
                    Debug.LogError("Prefab doesn't have Image component for thumbnail!");
                }
                
                Button testButton = imageButtonPrefab.GetComponent<Button>();
                if (testButton == null) {
                    Debug.LogError("Prefab doesn't have Button component!");
                }
            }
        }
    }

    void OnDestroy()
    {
        Debug.Log("StageManager OnDestroy() called");
        if (saveManager != null)
        {
            saveManager.OnProgressUpdated -= HandleProgressUpdate;
        }
        StopAllCoroutines();
        StartCoroutine(CleanupResources());
    }
    
    private void HandleProgressUpdate(List<SaveManager.ColoringProgress> progress)
{
    Debug.Log($"HandleProgressUpdate called with {progress.Count} entries");
    
    // Only refresh if we're not already doing so and there are actual changes
    if (!isRefreshing && progress.Count > 0)
    {
        // Use a small delay to prevent rapid successive refreshes
        StartCoroutine(DelayedRefresh(0.5f));
    }
}

    private IEnumerator DelayedRefresh(float delay)
{
    yield return new WaitForSeconds(delay);
    RefreshImageList();
}
    private void OnDisable()
    {
        Debug.Log("StageManager OnDisable() called");
        StartCoroutine(CleanupResources());
    }

    private IEnumerator CleanupResources()
    {
        Debug.Log("Cleaning up resources");
        foreach (var button in buttonCache.Values)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        buttonCache.Clear();
        
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return null;
        Debug.Log("Resources cleanup completed");
    }

    private void RefreshImageList()
    {
        Debug.Log("RefreshImageList called");
        StopAllCoroutines();
        StartCoroutine(RefreshImageListCoroutine());
    }

    private IEnumerator RefreshImageListCoroutine()
{
    if (isRefreshing) yield break;
    isRefreshing = true;

    // Xóa tất cả button cũ
    if (containerSettings.Length > 0 && containerSettings[0].container != null)
    {
        foreach (Transform child in containerSettings[0].container)
        {
            Destroy(child.gameObject);
        }
    }

    // Lấy danh sách progress
    var progressList = saveManager.GetAllProgress()
        .Where(p => !string.IsNullOrEmpty(p.imageId) && 
               !string.IsNullOrEmpty(p.savedPath) && 
               File.Exists(p.savedPath))
        .ToList();

    for (int i = 0; i < containerSettings.Length; i++) 
{
    if (i < progressList.Count) // Chỉ tạo nếu có đủ progress items
    {
        yield return StartCoroutine(
            CreateButtonForProgress(
                containerSettings[i].container, 
                progressList[i] // Lấy progress tương ứng với container index
            )
        );
    }
}

    isRefreshing = false;
    Debug.Log("Đã tạo 1 button duy nhất");
}
    
    private IEnumerator CreateButtonForProgress(Transform container, SaveManager.ColoringProgress progress)
    {
        try {
            // Tạo button cho hình đã lưu
            GameObject buttonObj = Instantiate(imageButtonPrefab, container);
            if (buttonObj == null) {
                Debug.LogError($"Không thể tạo button cho {progress.imageId}. Prefab là null.");
                yield break;
            }
            
            buttonObj.name = $"Button_{progress.imageId}";
            buttonObj.SetActive(true);
            
            // Lấy ImageSelectButton component
            ImageSelectButton buttonScript = buttonObj.GetComponent<ImageSelectButton>();
            if (buttonScript == null) {
                Debug.LogError($"Không tìm thấy ImageSelectButton component trên prefab");
                Destroy(buttonObj);
                yield break;
            }
            
            // Đảm bảo có thumbnailImage component
            if (buttonScript.thumbnailImage == null) {
                buttonScript.thumbnailImage = buttonObj.GetComponentInChildren<Image>();
                if (buttonScript.thumbnailImage == null) {
                    Debug.LogError($"Không tìm thấy Image component trên prefab");
                    Destroy(buttonObj);
                    yield break;
                }
            }
            
            // Load texture từ file đã lưu
            try {
                byte[] fileData = File.ReadAllBytes(progress.savedPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                
                if (texture.LoadImage(fileData))
                {
                    Debug.Log($"Đã load thành công texture cho {progress.imageId}");
                    buttonScript.Setup(
                        progress.imageId,
                        texture,
                        progress.lastSaved.ToString("dd/MM/yyyy HH:mm")
                    );
                    
                    // Thêm click handler
                    Button button = buttonObj.GetComponent<Button>();
                    if (button != null) {
                        string imageId = progress.imageId; // Tạo biến local để tránh closure
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => LoadImageById(imageId));
                    }
                    
                    buttonCache[progress.imageId] = buttonScript;
                    activeButtons.Add(buttonScript);
                }
                else
                {
                    Debug.LogError($"Không thể load texture cho {progress.imageId}");
                    Destroy(buttonObj);
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Lỗi khi xử lý texture cho {progress.imageId}: {ex.Message}");
                Destroy(buttonObj);
            }
        }
        catch (Exception ex) {
            Debug.LogError($"Lỗi khi xử lý {progress.imageId}: {ex.Message}");
        }
        
        yield return null;
    }
    
    private IEnumerator CreateButtonForTemplate(Transform container, string templateId)
    {
        if (buttonCache.ContainsKey(templateId))
    {
        Debug.Log($"Button for {templateId} already exists, skipping creation");
        yield break;
    }
        try {
            // Tạo button cho template
            GameObject buttonObj = Instantiate(imageButtonPrefab, container);
            buttonObj.name = $"Button_{templateId}";
            buttonObj.SetActive(true);
            
            ImageSelectButton buttonScript = buttonObj.GetComponent<ImageSelectButton>();
            if (buttonScript == null) {
                Debug.LogError($"Không tìm thấy ImageSelectButton component trên prefab");
                Destroy(buttonObj);
                yield break;
            }
            
            // Đảm bảo có thumbnailImage component
            if (buttonScript.thumbnailImage == null) {
                buttonScript.thumbnailImage = buttonObj.GetComponentInChildren<Image>();
                if (buttonScript.thumbnailImage == null) {
                    Debug.LogError($"Không tìm thấy Image component trên prefab");
                    Destroy(buttonObj);
                    yield break;
                }
            }
            
            // Load template texture từ Resources hoặc nơi lưu trữ template
            Texture2D texture = LoadTemplateTexture(templateId);
            if (texture != null)
            {
                Debug.Log($"Đã load thành công template texture cho {templateId}");
                buttonScript.Setup(
                    templateId,
                    texture,
                    "Chưa tô màu" // Hiển thị nhãn "Chưa tô màu" thay vì ngày giờ
                );
                
                // Thêm click handler
                Button button = buttonObj.GetComponent<Button>();
                if (button != null) {
                    string imageId = templateId; // Tạo biến local để tránh closure
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => LoadImageById(imageId));
                }
                
                buttonCache[templateId] = buttonScript;
                activeButtons.Add(buttonScript);
            }
            else
            {
                Debug.LogError($"Không thể load template texture cho {templateId}");
                Destroy(buttonObj);
            }
        }
        catch (Exception ex) {
            Debug.LogError($"Lỗi khi xử lý template {templateId}: {ex.Message}");
        }
        
        yield return null;
    }

    private Texture2D LoadTemplateTexture(string templateId)
    {
        // Implement theo cách bạn lưu trữ template images
        // Ví dụ:
        return Resources.Load<Texture2D>($"ColoringTemplates/{templateId}");
        
        // Hoặc nếu bạn có cách khác để load:
        // return YourCustomLoadMethod(templateId);
    }
    
    public void RefreshAfterSave()
    {
        // Call this after a new save is created
        if (!isRefreshing)
        {
            StartCoroutine(RefreshImageListCoroutine());
        }
    }

    private List<string> GetTemplateImageIds()
    {
        // Implement theo cách bạn lưu trữ template images
        // Ví dụ:
        List<string> templateIds = new List<string>();
        
        // Nếu template nằm trong Resources folder
        Texture2D[] templates = Resources.LoadAll<Texture2D>("ColoringTemplates");
        foreach (var template in templates)
        {
            templateIds.Add(template.name);
        }
        
        // Hoặc nếu bạn có danh sách cố định
        // templateIds.AddRange(new[] { "image_1", "image_2", "image_3" });
        
        return templateIds;
    }
    
    public void LoadImageById(string imageId)
    {
        Debug.Log($"LoadImageById called for image ID: {imageId}");
        StartCoroutine(LoadImageCoroutine(imageId));
    }

    private IEnumerator LoadImageCoroutine(string imageId)
    {
        Debug.Log($"Starting to load image ID: {imageId}");
        PlayerPrefs.SetString("SelectedImageId", imageId);
        PlayerPrefs.Save();
        
        // Ensure cleanup before scene change
        StopAllCoroutines();
        yield return StartCoroutine(CleanupResources());
        
        Debug.Log($"Loading ColoringScene with image ID: {imageId}");
        UnityEngine.SceneManagement.SceneManager.LoadScene("ColoringScene");
    }
}