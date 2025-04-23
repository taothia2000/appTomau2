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
        // Xóa CleanupResources() ở đây không ảnh hưởng đến chạy
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
        // Xóa CleanupResources() ở đây không ảnh hưởng đến chạy
    }

    // Xóa hàm CleanupResources() không ảnh hưởng đến chạy

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

        Debug.Log("Starting to refresh image list");

        // Clear all existing buttons
        foreach (var settings in containerSettings)
        {
            if (settings.container != null)
            {
                foreach (Transform child in settings.container)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // Get all valid progress items
        var progressList = saveManager.GetAllProgress()
            .Where(p => !string.IsNullOrEmpty(p.imageId) && 
                !string.IsNullOrEmpty(p.savedPath) && 
                File.Exists(p.savedPath))
            .ToList();

        // Sort by ID to ensure correct ordering
        progressList.Sort((a, b) => {
            // Extract numeric part from imageId (e.g., "image_010" becomes 10)
            int idA = ExtractImageNumber(a.imageId);
            int idB = ExtractImageNumber(b.imageId);
            return idA.CompareTo(idB);
        });

        Debug.Log($"Found {progressList.Count} valid progress items");

        // Process each image
        foreach (var progress in progressList)
        {
            // Extract the image number
            int imageNumber = ExtractImageNumber(progress.imageId);
            Debug.Log($"Processing image ID: {progress.imageId}, Number: {imageNumber}");
            
            // Find appropriate container
            Transform targetContainer = FindContainerForImage(imageNumber);
            
            if (targetContainer != null)
            {
                Debug.Log($"Creating button for {progress.imageId} in container {targetContainer.name}");
                yield return StartCoroutine(CreateButtonForProgress(targetContainer, progress));
            }
            else
            {
                Debug.LogWarning($"No container found for image {progress.imageId} (number {imageNumber})");
            }
        }

        isRefreshing = false;
        Debug.Log($"Grid populated with {progressList.Count} buttons");
    }

    private Transform FindContainerForImage(int imageNumber)
    {
        // First try to find an exact match by container name
        foreach (var settings in containerSettings)
        {
            if (settings.container == null) continue;
            
            string containerName = settings.container.name.ToLower();
            
            // Check for exact match (e.g., "Image10" for image 10)
            if (containerName == $"image{imageNumber}" || 
                containerName == $"image{imageNumber}".ToLower() || 
                containerName == $"image_{imageNumber}".ToLower())
            {
                Debug.Log($"Found exact container match: {settings.container.name} for image {imageNumber}");
                return settings.container;
            }
        }
        
        // If no exact match, try to find by range
        // Example: Images 1-5 go to the first container, etc.
        if (imageNumber >= 1 && imageNumber <= 5 && containerSettings.Length > 0)
        {
            Debug.Log($"Using first container for image {imageNumber}");
            return containerSettings[0].container;
        }
        else if (imageNumber >= 6 && imageNumber <= 10 && containerSettings.Length > 1)
        {
            Debug.Log($"Using second container for image {imageNumber}");
            return containerSettings[1].container;
        }
        else if (imageNumber >= 11 && containerSettings.Length > 2)
        {
            Debug.Log($"Using third container for image {imageNumber}");
            return containerSettings[2].container;
        }
        
        // Default to first container if available
        if (containerSettings.Length > 0 && containerSettings[0].container != null)
        {
            Debug.Log($"Using default (first) container for image {imageNumber}");
            return containerSettings[0].container;
        }
        
        Debug.LogError($"No suitable container found for image {imageNumber}");
        return null;
    }

    private int ExtractImageNumber(string imageId)
    {
        // Handle both formats: "imageX" and "image_X"
        string pattern = @"(\d+)";
        var match = System.Text.RegularExpressions.Regex.Match(imageId, pattern);
        
        if (match.Success && int.TryParse(match.Value, out int number))
        {
            return number;
        }
        return 0;
    }


    private IEnumerator CreateButtonForProgress(Transform container, SaveManager.ColoringProgress progress)
    {
        try {
            // Create button and parent it immediately
            GameObject buttonObj = Instantiate(imageButtonPrefab, container, false);
            buttonObj.name = $"Button_{progress.imageId}";
            
            // Ensure button has correct size to fit in grid cell
            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Find container settings
                var settings = containerSettings.FirstOrDefault(s => s.container == container);
                if (settings != null)
                {
                    // Ensure button fits grid cell exactly
                    rt.sizeDelta = settings.buttonSize;
                }
            }
            
            buttonObj.SetActive(true);
            
            // Rest of the method unchanged...
            ImageSelectButton buttonScript = buttonObj.GetComponent<ImageSelectButton>();
            if (buttonScript == null) {
                Debug.LogError($"ImageSelectButton component not found on prefab");
                Destroy(buttonObj);
                yield break;
            }
            
            // Ensure thumbnailImage component exists
            if (buttonScript.thumbnailImage == null) {
                buttonScript.thumbnailImage = buttonObj.GetComponentInChildren<Image>();
                if (buttonScript.thumbnailImage == null) {
                    Debug.LogError($"Image component not found on prefab");
                    Destroy(buttonObj);
                    yield break;
                }
            }
            
            // Load texture from saved file
            try {
                byte[] fileData = File.ReadAllBytes(progress.savedPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                
                if (texture.LoadImage(fileData))
                {
                    Debug.Log($"Successfully loaded texture for {progress.imageId}");
                    buttonScript.Setup(
                        progress.imageId,
                        texture,
                        progress.lastSaved.ToString("dd/MM/yyyy HH:mm")
                    );
                    
                    // Add click handler
                    Button button = buttonObj.GetComponent<Button>();
                    if (button != null) {
                        string imageId = progress.imageId;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => LoadImageById(imageId));
                    }
                    
                    buttonCache[progress.imageId] = buttonScript;
                    activeButtons.Add(buttonScript);
                }
                else
                {
                    Debug.LogError($"Could not load texture for {progress.imageId}");
                    Destroy(buttonObj);
                }
            }
            catch (Exception ex) {
                Debug.LogError($"Error processing texture for {progress.imageId}: {ex.Message}");
                Destroy(buttonObj);
            }
        }
        catch (Exception ex) {
            Debug.LogError($"Error processing {progress.imageId}: {ex.Message}");
        }
        
        yield return null;
    }

    public void RefreshAfterSave()
    {
        // Call this after a new save is created
        if (!isRefreshing)
        {
            StartCoroutine(RefreshImageListCoroutine());
        }
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
        
        // Ensure cleanup before scene change - không cần thiết nhưng vẫn có thể thực hiện giải phóng cơ bản
        StopAllCoroutines();
        
        Debug.Log($"Loading ColoringScene with image ID: {imageId}");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        yield return null;
    }
}