using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            DontDestroyOnLoad(gameObject); // Giữ đối tượng này khi chuyển scene
        }
        else
        {
            Destroy(gameObject); // Đảm bảo chỉ có một instance duy nhất
        }

        // Đảm bảo thư mục lưu tồn tại
        if (!Directory.Exists(imageSaveFolder))
        {
            Directory.CreateDirectory(imageSaveFolder);
        }

        LoadProgress();
    }
    
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

    public void SaveImage(string imageId, Texture2D texture)
    {
        Debug.Log($"Đang lưu texture ID {imageId} vào cache...");
        Texture2D textureCopy = new Texture2D(texture.width, texture.height);
        textureCopy.SetPixels(texture.GetPixels());
        textureCopy.Apply();

        if (textureCache.ContainsKey(imageId))
        {
            textureCache[imageId] = textureCopy;
        }
        else
        {
            textureCache.Add(imageId, textureCopy);
        }

        UpdateProgress(imageId, "");

        Debug.Log($"Đã lưu texture ID {imageId} vào cache");
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
        if (OnProgressUpdated != null)
        {
            OnProgressUpdated.Invoke(progressData.savedImages);
        }
    }
    
    public void MarkAsCompleted(string imageId)
    {
        ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
        if (progress != null)
        {
            progress.isCompleted = true;
            SaveProgress();
            
            // Kích hoạt event cập nhật UI
            if (OnProgressUpdated != null)
            {
                OnProgressUpdated.Invoke(progressData.savedImages);
            }
        }
    }
    
    public List<ColoringProgress> GetAllProgress()
    {
        return progressData.savedImages;
    }
    
   public Texture2D LoadSavedImage(string imageId)
{
    Debug.Log($"Đang tải texture ID {imageId} từ cache...");
    if (textureCache.ContainsKey(imageId))
    {
        Debug.Log($"Texture ID {imageId} được tìm thấy trong cache.");
        return textureCache[imageId];
    }

    ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
    if (progress != null)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        if (File.Exists(progress.savedPath))
        {
            byte[] fileData = File.ReadAllBytes(progress.savedPath);
            if (texture.LoadImage(fileData))
            {
                Debug.Log($"Đã tải texture ID {imageId} từ file.");
                textureCache[imageId] = texture;
                return texture;
            }
        }
    }
    Debug.LogError($"Không thể tải texture ID {imageId}.");
    return null;
}
    
    private void LoadProgress()
    {
        if (File.Exists(saveDataPath))
        {
            try
            {
                string json = File.ReadAllText(saveDataPath);
                progressData = JsonUtility.FromJson<ProgressData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi đọc dữ liệu tiến độ: {e.Message}");
                progressData = new ProgressData();
            }
        }
        else
        {
            progressData = new ProgressData();
        }
    }
    
    public void SaveProgress()
{
    try
    {
        string json = JsonUtility.ToJson(progressData, true);
        File.WriteAllText(saveDataPath, json);
        
        // Kích hoạt event để cập nhật UI
        if (OnProgressUpdated != null)
        {
            OnProgressUpdated.Invoke(progressData.savedImages);
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Lỗi khi lưu dữ liệu tiến độ: {e.Message}");
    }
}
    
}