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
    
    public Transform[] buttonContainers;
    public SaveManager saveManager;
    
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
        StartCoroutine(InitializeManager());
        saveManager.CleanupInvalidProgressEntries();
    }

    private IEnumerator InitializeManager()
    {
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
        
        yield return StartCoroutine(RefreshImageListCoroutine());
            
        isInitialized = true;
    }

    private void InitializeSaveManager()
    {
        saveManager = SaveManager.Instance;
        if (saveManager == null)
        {
            return;
        }
        saveManager.OnProgressUpdated += HandleProgressUpdate;
        PlayerPrefs.DeleteKey("SelectedImageId");
        
        var allProgress = saveManager.GetAllProgress();
        
    }

    private void ConfigureGridLayouts()
    {
        
        if (containerSettings == null || containerSettings.Length == 0)
        {
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
                return;
            }
        }
        
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
        
    }
    
    void Awake()
    {
        LoadImageButtonPrefab();
    }

    private void LoadImageButtonPrefab()
    {
        if (imageButtonPrefab == null)
        {
            
            imageButtonPrefab = Resources.Load<GameObject>("Prefabs/ImageSelectButton");
            
            if (imageButtonPrefab == null)
            {
                _ = Resources.LoadAll<GameObject>("");

            }
            else
            {
                _ = imageButtonPrefab.GetComponent<ImageSelectButton>();
                _ = imageButtonPrefab.GetComponentInChildren<Image>();
                _ = imageButtonPrefab.GetComponent<Button>();

            }
        }
    }

    void OnDestroy()
    {
        if (saveManager != null)
        {
            saveManager.OnProgressUpdated -= HandleProgressUpdate;
        }
        StopAllCoroutines();
    }
    
    private void HandleProgressUpdate(List<SaveManager.ColoringProgress> progress)
    {
        if (!isRefreshing && progress.Count > 0)
        {
            StartCoroutine(DelayedRefresh(0.5f));
        }
    }

    private IEnumerator DelayedRefresh(float delay)
    {
        yield return new WaitForSeconds(delay);
        RefreshImageList();
    }
    private void RefreshImageList()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshImageListCoroutine());
    }

    private IEnumerator RefreshImageListCoroutine()
{
    if (isRefreshing) yield break;
    isRefreshing = true;

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

    // Xác định danh sách ID hợp lệ
    int minId = int.MaxValue;
    int maxId = int.MinValue;
    foreach (var settings in containerSettings)
    {
        if (settings.container == null) continue;
        
        string containerName = settings.container.name.ToLower();
        string pattern = @"(\d+)";
        var match = System.Text.RegularExpressions.Regex.Match(containerName, pattern);
        if (match.Success && int.TryParse(match.Value, out int containerId))
        {
            minId = Mathf.Min(minId, containerId);
            maxId = Mathf.Max(maxId, containerId);
        }
    }

    var progressList = saveManager.GetAllProgress()
        .Where(p => !string.IsNullOrEmpty(p.imageId) && 
                    !string.IsNullOrEmpty(p.savedPath) && 
                    File.Exists(p.savedPath))
        .ToList();

    progressList.Sort((a, b) => {
        int idA = ExtractImageNumber(a.imageId);
        int idB = ExtractImageNumber(b.imageId);
        return idA.CompareTo(idB);
    });

    foreach (var progress in progressList)
    {
        int imageNumber = ExtractImageNumber(progress.imageId);
        
        // Chỉ xử lý nếu imageNumber nằm trong khoảng hợp lệ
        if (imageNumber < minId || imageNumber > maxId)
        {
            Debug.LogWarning($"Skipping imageId: {progress.imageId} as it is outside valid container range ({minId}-{maxId}).");
            continue;
        }

        Transform targetContainer = FindContainerForImage(imageNumber);
        if (targetContainer != null)
        {
            yield return StartCoroutine(CreateButtonForProgress(targetContainer, progress));
        }
    }

    isRefreshing = false;
}

   private Transform FindContainerForImage(int imageNumber)
{
    // Xác định danh sách ID hợp lệ (dựa trên containerSettings)
    int minId = int.MaxValue;
    int maxId = int.MinValue;
    
    foreach (var settings in containerSettings)
    {
        if (settings.container == null) continue;
        
        string containerName = settings.container.name.ToLower();
        // Trích xuất số từ tên container (ví dụ: "image17" -> 17)
        string pattern = @"(\d+)";
        var match = System.Text.RegularExpressions.Regex.Match(containerName, pattern);
        if (match.Success && int.TryParse(match.Value, out int containerId))
        {
            minId = Mathf.Min(minId, containerId);
            maxId = Mathf.Max(maxId, containerId);
        }
    }

    // Kiểm tra nếu imageNumber không nằm trong khoảng hợp lệ
    if (imageNumber < minId || imageNumber > maxId)
    {
        Debug.LogWarning($"Image number {imageNumber} is outside valid container range ({minId}-{maxId}). Skipping.");
        return null;
    }

    // Tìm container khớp
    foreach (var settings in containerSettings)
    {
        if (settings.container == null) continue;
        
        string containerName = settings.container.name.ToLower();
        if (containerName == $"image{imageNumber}" || 
            containerName == $"image{imageNumber}".ToLower() || 
            containerName == $"image_{imageNumber}".ToLower())
        {
            return settings.container;
        }
    }
    
    Debug.LogError($"No valid container found for image number: {imageNumber}. Skipping.");
    return null;
}

    private int ExtractImageNumber(string imageId)
    {
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
    if (container == null)
    {
        Debug.LogWarning($"No valid container for imageId: {progress.imageId}. Skipping button creation.");
        yield break;
    }

    try
    {
        GameObject buttonObj = Instantiate(imageButtonPrefab, container, false);
        buttonObj.name = $"Button_{progress.imageId}";
        
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            var settings = containerSettings.FirstOrDefault(s => s.container == container);
            if (settings != null)
            {
                rt.sizeDelta = settings.buttonSize;
            }
        }
        
        buttonObj.SetActive(true);
        
        ImageSelectButton buttonScript = buttonObj.GetComponent<ImageSelectButton>();
        if (buttonScript == null)
        {
            Destroy(buttonObj);
            yield break;
        }
        
        if (buttonScript.thumbnailImage == null)
        {
            buttonScript.thumbnailImage = buttonObj.GetComponentInChildren<Image>();
            if (buttonScript.thumbnailImage == null)
            {
                Destroy(buttonObj);
                yield break;
            }
        }
        
        try
        {
            byte[] fileData = File.ReadAllBytes(progress.savedPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            
            if (texture.LoadImage(fileData))
            {
                buttonScript.Setup(
                    progress.imageId,
                    texture,
                    progress.lastSaved.ToString("dd/MM/yyyy HH:mm")
                );
                
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    string imageId = progress.imageId;
                    button.onClick.AddListener(() => LoadImageById(imageId));
                }
                
                buttonCache[progress.imageId] = buttonScript;
                activeButtons.Add(buttonScript);
            }
            else
            {
                Destroy(buttonObj);
            }
        }
        catch (Exception)
        {
            Destroy(buttonObj);
        }
    }
    catch (Exception)
    {
    }
    
    yield return null;
}

    public void RefreshAfterSave()
    {
        if (!isRefreshing)
        {
            StartCoroutine(RefreshImageListCoroutine());
        }
    }

    public void LoadImageById(string imageId)
    {
        StartCoroutine(LoadImageCoroutine(imageId));
    }

    private IEnumerator LoadImageCoroutine(string imageId)
    {
        PlayerPrefs.SetString("SelectedImageId", imageId);
        PlayerPrefs.Save();
        
        StopAllCoroutines();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        yield return null;
    }
}