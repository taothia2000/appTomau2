using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    [Serializable]
    public class ColoringProgress
    {
        public string imageId;
        public string savedPath;
        public DateTime lastSaved;
        //public bool isCompleted;
    }
    
    [Serializable]
    public class ProgressData
    {
        public List<ColoringProgress> savedImages = new List<ColoringProgress>();
    }
    
    private ProgressData progressData;
    public string saveDataPath => Path.Combine(Application.persistentDataPath, "coloring_progress.json");
    public string imageSaveFolder => Path.Combine(Application.persistentDataPath, "SavedImages");
    
    // Event cho UI cập nhật
    public System.Action<List<ColoringProgress>> OnProgressUpdated;
    
    private void Awake()
    {
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeManager();
        
        // Load existing progress right away
        if (HasSaveFile())
        {
            LoadProgress();
        }
    }
    else
    {
        Destroy(gameObject);
    }
}
    
     private void InitializeManager()
    {
        // Initialize with new ProgressData
        progressData = new ProgressData();
        
        // Ensure save directory exists
        if (!Directory.Exists(imageSaveFolder))
        {
            Directory.CreateDirectory(imageSaveFolder);
        }
        
    }
    public void CleanupInvalidProgressEntries()
{
    List<ColoringProgress> validEntries = new List<ColoringProgress>();
    
    foreach (var entry in progressData.savedImages)
    {
        if (!string.IsNullOrEmpty(entry.imageId) && 
            !string.IsNullOrEmpty(entry.savedPath) && 
            File.Exists(entry.savedPath))
        {
            validEntries.Add(entry);
        }
        else
        {
            Debug.Log($"Removing invalid entry: ID={entry.imageId}, Path={entry.savedPath}");
        }
    }
    
    progressData.savedImages = validEntries;
    SaveProgress();
    Debug.Log($"Cleanup complete. Valid entries: {validEntries.Count}");
}

    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private Dictionary<string, DateTime> lastSaveTimeByImageId = new Dictionary<string, DateTime>();
    private Dictionary<string, Coroutine> pendingSaves = new Dictionary<string, Coroutine>();
    private float saveDelay = 1.0f;
    private readonly float MIN_SAVE_INTERVAL = 5.0f;
    public void CleanupCache()
{
    // Tạo danh sách các key để tránh lỗi khi duyệt Dictionary
    List<string> keys = new List<string>(textureCache.Keys);
    
    foreach (var key in keys)
    {
        if (textureCache[key] != null)
        {
            Destroy(textureCache[key]);
        }
    }
    
    textureCache.Clear();
    Debug.Log("Đã làm sạch texture cache");
    
    // Ép Unity giải phóng bộ nhớ
    Resources.UnloadUnusedAssets();
    System.GC.Collect();
}
public void QueueSaveImage(string imageId, Texture2D texture)
    {
        // Hủy yêu cầu lưu đang chờ xử lý (nếu có)
        if (pendingSaves.ContainsKey(imageId) && pendingSaves[imageId] != null)
        {
            StopCoroutine(pendingSaves[imageId]);
        }
        
        // Tạo yêu cầu lưu mới
        pendingSaves[imageId] = StartCoroutine(SaveWithDelay(imageId, texture));
    }
    
    private IEnumerator SaveWithDelay(string imageId, Texture2D texture)
    {
        // Chờ một khoảng thời gian để người dùng có thể tiếp tục tương tác
        yield return new WaitForSeconds(saveDelay);
        
        // Thực hiện lưu
        SaveImage(imageId, texture);
        
        // Xóa khỏi danh sách đang chờ
        pendingSaves.Remove(imageId);
    }

    public bool HasSaveFile()
    {
        return File.Exists(saveDataPath);
    }
    private void OnLowMemory()
{
    // Clear cache when system is low on memory
    ClearCache();
}

public void ClearUnusedTextures(List<string> activeIds)
{
    List<string> keysToRemove = new List<string>();
    
    foreach (var key in textureCache.Keys)
    {
        if (!activeIds.Contains(key))
        {
            if (textureCache[key] != null)
            {
                Destroy(textureCache[key]);
            }
            keysToRemove.Add(key);
        }
    }
    
    foreach (var key in keysToRemove)
    {
        textureCache.Remove(key);
    }
    
    // Log thông tin
    Debug.Log($"Đã xóa {keysToRemove.Count} texture không sử dụng khỏi bộ nhớ");
}

  public void SaveImage(string imageId, Texture2D texture)
{
    if (string.IsNullOrEmpty(imageId) || texture == null)
    {
        Debug.LogError("Invalid imageId or texture is null");
        return;
    }

    try {
        // Đảm bảo imageId là duy nhất và hợp lệ
        if (!imageId.StartsWith("image_"))
        {
            imageId = "image_" + imageId;
        }
            
        // Tạo tên file cố định cho mỗi ID
        string fileName = $"{imageId}.png"; 
        string filePath = Path.Combine(imageSaveFolder, fileName);
            
        // Tạo dữ liệu PNG từ texture
        byte[] pngData = texture.EncodeToPNG();
            
        // Lưu vào file
        File.WriteAllBytes(filePath, pngData);
            
        // Cập nhật thông tin trong dữ liệu tiến trình
        UpdateProgress(imageId, filePath);
            
        Debug.Log($"Đã lưu hình ảnh cho ID: {imageId} tại đường dẫn: {filePath}");
    }
    catch (Exception e) {
        Debug.LogError($"Lỗi khi lưu texture: {e.Message}");
    }
}

    private void UpdateProgress(string imageId, string filePath)
    {
        // Tìm tiến độ hiện có
        ColoringProgress existingProgress = progressData.savedImages.Find(p => p.imageId == imageId);
        
        if (existingProgress != null)
        {
            // Cập nhật tiến độ hiện có
            existingProgress.savedPath = filePath;
            existingProgress.lastSaved = DateTime.Now;
        }
        else
        {
            // Tạo tiến độ mới
            ColoringProgress newProgress = new ColoringProgress
            {
                imageId = imageId,
                savedPath = filePath,
                lastSaved = DateTime.Now,
                //isCompleted = false
            };
            
            progressData.savedImages.Add(newProgress);
        }
        
        // Lưu dữ liệu tiến độ
        SaveProgress();
        
        // Kích hoạt event cập nhật UI
        OnProgressUpdated?.Invoke(progressData.savedImages);
    }
    
    // public void MarkAsCompleted(string imageId)
    // {
    //     ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
    //     if (progress != null)
    //     {
    //         progress.isCompleted = true;
    //         SaveProgress();
            
    //         // Kích hoạt event cập nhật UI
    //         OnProgressUpdated?.Invoke(progressData.savedImages);
    //     }
    // }
    
    public List<ColoringProgress> GetAllProgress()
{
    // Nếu chưa load data và có file save, thì load
    if (progressData.savedImages.Count == 0 && HasSaveFile())
    {
        LoadProgress();
    }
    
    // Log all IDs to check for duplicates
    Debug.Log("All image IDs:");
    foreach (var progress in progressData.savedImages)
    {
        Debug.Log($"ID: {progress.imageId}, Path: {progress.savedPath}, LastSaved: {progress.lastSaved}");
    }
    
    return progressData.savedImages;
}
    
    public Texture2D LoadSavedImage(string imageId)
{
    if (string.IsNullOrEmpty(imageId))
    {
        Debug.LogError("LoadSavedImage: ID trống!");
        return null;
    }
    
    // Xóa khỏi cache trước để đảm bảo load mới
    if (textureCache.ContainsKey(imageId) && textureCache[imageId] != null)
    {
        Destroy(textureCache[imageId]);
        textureCache.Remove(imageId);
    }
    
    // Find the progress entry for this imageId
    ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
    
    if (progress != null && !string.IsNullOrEmpty(progress.savedPath) && File.Exists(progress.savedPath))
    {
        try
        {
            Debug.Log($"Loading image from path: {progress.savedPath}");
            byte[] fileData = File.ReadAllBytes(progress.savedPath);
            
            // Luôn tạo texture mới
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = $"Texture_{imageId}"; // Đặt tên cho texture để dễ debug
            
            if (texture.LoadImage(fileData))
            {
                Debug.Log($"Texture loaded successfully for image ID: {imageId}, Size: {texture.width}x{texture.height}");
                
                // Kiểm tra pixel để đảm bảo texture có dữ liệu
                try {
                    Color topLeftPixel = texture.GetPixel(0, 0);
                    Debug.Log($"Top-left pixel color: R={topLeftPixel.r}, G={topLeftPixel.g}, B={topLeftPixel.b}, A={topLeftPixel.a}");
                }
                catch (Exception pixelEx) {
                    Debug.LogWarning($"Could not read pixel data: {pixelEx.Message}");
                }
                
                textureCache[imageId] = texture;
                return texture;
            }
            else
            {
                Debug.LogError($"Failed to load image data for {imageId}");
                Destroy(texture);
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading image: {e.Message}");
        }
    }
    
    Debug.LogWarning($"No saved file found for imageId: {imageId}");
    return null;
}

    private string GetSavedImagePath(string imageId)
{
    ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
    return progress != null ? progress.savedPath : null;
}

    private void LoadProgress()
    {
        Debug.Log("=== LOADING PROGRESS ===");
        Debug.Log($"Looking for save file at: {saveDataPath}");
        
        try
        {
            string json = File.ReadAllText(saveDataPath);
            Debug.Log($"Loaded JSON: {json}");
            progressData = JsonUtility.FromJson<ProgressData>(json);
            Debug.Log($"Loaded {progressData.savedImages.Count} saved images");
        }
        catch (Exception e)
        {
            Debug.LogError($"Load Progress Error: {e.Message}\n{e.StackTrace}");
            progressData = new ProgressData();
        }
    }
    
    public void SaveProgress()
    {
        try
        {
            Debug.Log("=== SAVING PROGRESS ===");
            Debug.Log($"Number of saved images: {progressData.savedImages.Count}");
            string json = JsonUtility.ToJson(progressData, true);
            File.WriteAllText(saveDataPath, json);
            Debug.Log($"Progress saved to: {saveDataPath}");
            Debug.Log($"JSON Content: {json}");
            
            OnProgressUpdated?.Invoke(progressData.savedImages);
            Debug.Log("Progress update event invoked");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save Progress Error: {e.Message}\n{e.StackTrace}");
        }
    }

    internal void ClearCache()
{
    foreach (var key in textureCache.Keys)
    {
        if (textureCache[key] != null)
        {
            Destroy(textureCache[key]);
        }
    }
    textureCache.Clear();
}
// theo dõi bộ nhớ
private void LogMemoryUsage(string label)
{
    #if UNITY_EDITOR
    float memoryInMB = (float)System.GC.GetTotalMemory(false) / (1024 * 1024);
    Debug.Log($"Memory usage ({label}): {memoryInMB:F2} MB");
    #endif
}

private readonly int MAX_CACHE_SIZE = 12; // Số lượng texture tối đa lưu trong cache

private void LimitCacheSize()
{
    if (textureCache.Count <= MAX_CACHE_SIZE)
        return;
        
    // Sắp xếp các key theo thời gian truy cập gần nhất (cần theo dõi thêm)
    var keysToRemove = textureCache.Keys.Take(textureCache.Count - MAX_CACHE_SIZE).ToList();
    
    foreach (var key in keysToRemove)
    {
        if (textureCache[key] != null)
        {
            Destroy(textureCache[key]);
        }
        textureCache.Remove(key);
    }
    
    Debug.Log($"Đã giới hạn cache xuống {MAX_CACHE_SIZE} texture");
}
public void PrepareForSceneChange()
{
    // Lưu lại các thay đổi
    SaveProgress();
    
    // Giải phóng bớt bộ nhớ nếu cần
    Resources.UnloadUnusedAssets();
}
}