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
        public bool isCompleted;
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

    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

    public bool HasSaveFile()
    {
        return File.Exists(saveDataPath);
    }

    public void SaveImage(string imageId, Texture2D texture)
    {
        if (string.IsNullOrEmpty(imageId) || texture == null)
        {
            Debug.LogError("Invalid imageId or texture is null");
            return;
        }

        try {
            // Look for existing progress
            ColoringProgress existingProgress = progressData.savedImages.Find(p => p.imageId == imageId);
            
            // Delete old file if it exists
            if (existingProgress != null && File.Exists(existingProgress.savedPath))
            {
                File.Delete(existingProgress.savedPath);
            }
            
            // Create a copy of the texture
            Texture2D textureCopy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            textureCopy.SetPixels(texture.GetPixels());
            textureCopy.Apply();
            
            // Update texture cache
            if (textureCache.ContainsKey(imageId))
            {
                textureCache[imageId] = textureCopy;
            }
            else
            {
                textureCache.Add(imageId, textureCopy);
            }
            
            // Create new filename with timestamp
            string fileName = $"{imageId}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(imageSaveFolder, fileName);
            
            // Save to file
            File.WriteAllBytes(filePath, textureCopy.EncodeToPNG());
            
            // Update progress record
            UpdateProgress(imageId, filePath);
            Debug.Log($"Saved texture ID {imageId} to file: {filePath}");
        }
        catch (Exception e) {
            Debug.LogError($"Error saving texture: {e.Message}");
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
                isCompleted = false
            };
            
            progressData.savedImages.Add(newProgress);
        }
        
        // Lưu dữ liệu tiến độ
        SaveProgress();
        
        // Kích hoạt event cập nhật UI
        OnProgressUpdated?.Invoke(progressData.savedImages);
    }
    
    public void MarkAsCompleted(string imageId)
    {
        ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
        if (progress != null)
        {
            progress.isCompleted = true;
            SaveProgress();
            
            // Kích hoạt event cập nhật UI
            OnProgressUpdated?.Invoke(progressData.savedImages);
        }
    }
    
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
        Debug.Log($"ID: {progress.imageId}, Path: {progress.savedPath}, Completed: {progress.isCompleted}");
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
    
    // Check cache first
    if (textureCache.ContainsKey(imageId) && textureCache[imageId] != null)
    {
        Debug.Log($"Đã lưu texture ID {imageId} vào cache");
        return textureCache[imageId];
    }
    
    string folderPath = imageSaveFolder;
    
    // Tìm tất cả file có tên chứa ID này
    string[] files = Directory.GetFiles(folderPath, $"{imageId}*.png");
    
    Debug.Log($"Tìm kiếm hình ảnh với ID {imageId} tại thư mục {folderPath}");
    Debug.Log($"Tìm thấy {files.Length} file");
    
    if (files.Length > 0)
    {
        // Lấy file gần đây nhất (giả sử đặt tên file có chứa timestamp)
        string latestFile = files.OrderByDescending(f => f).First();
        Debug.Log($"Đang tải hình ảnh từ: {latestFile}");
        
        try
        {
            byte[] fileData = File.ReadAllBytes(latestFile);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            
            if (texture.LoadImage(fileData))
            {
                Debug.Log($"Tải thành công texture với kích thước {texture.width}x{texture.height}");
                // Add to cache
                textureCache[imageId] = texture;
                return texture;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Lỗi khi tải hình ảnh: {e.Message}");
        }
    }
    
    Debug.Log($"Không tìm thấy hình ảnh nào cho ID: {imageId}");
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
         textureCache.Clear();
    }
}