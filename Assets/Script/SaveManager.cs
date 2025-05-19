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
        progressData = new ProgressData();
        
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
            }
        }
        
        progressData.savedImages = validEntries;
        SaveProgress();
    }

    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private Dictionary<string, DateTime> lastSaveTimeByImageId = new Dictionary<string, DateTime>();
    private Dictionary<string, Coroutine> pendingSaves = new Dictionary<string, Coroutine>();
    private float saveDelay = 1.0f;
    private readonly float MIN_SAVE_INTERVAL = 5.0f;
    
    
    public void QueueSaveImage(string imageId, Texture2D texture)
    {
        if (pendingSaves.ContainsKey(imageId) && pendingSaves[imageId] != null)
        {
            StopCoroutine(pendingSaves[imageId]);
        }
        
        pendingSaves[imageId] = StartCoroutine(SaveWithDelay(imageId, texture));
    }
    
    private IEnumerator SaveWithDelay(string imageId, Texture2D texture)
    {
        yield return new WaitForSeconds(saveDelay);
        
        SaveImage(imageId, texture);
        
        pendingSaves.Remove(imageId);
    }

    public bool HasSaveFile()
    {
        return File.Exists(saveDataPath);
    }

    public void SaveImage(string imageId, Texture2D texture)
    {
        if (string.IsNullOrEmpty(imageId) || texture == null)
        {
            return;
        }

        try {
            if (!imageId.StartsWith("image_"))
            {
                imageId = "image_" + imageId;
            }
                
            string fileName = $"{imageId}.png"; 
            string filePath = Path.Combine(imageSaveFolder, fileName);
                
            byte[] pngData = texture.EncodeToPNG();
                
            File.WriteAllBytes(filePath, pngData);
            UpdateProgress(imageId, filePath);
                
        }
        catch (Exception ) {
        }
    }

    private void UpdateProgress(string imageId, string filePath)
    {
        ColoringProgress existingProgress = progressData.savedImages.Find(p => p.imageId == imageId);
        
        if (existingProgress != null)
        {
            existingProgress.savedPath = filePath;
            existingProgress.lastSaved = DateTime.Now;
        }
        else
        {
            ColoringProgress newProgress = new ColoringProgress
            {
                imageId = imageId,
                savedPath = filePath,
                lastSaved = DateTime.Now,
                //isCompleted = false
            };
            
            progressData.savedImages.Add(newProgress);
        }
        
        SaveProgress();
        
        OnProgressUpdated?.Invoke(progressData.savedImages);
    }
    
    public List<ColoringProgress> GetAllProgress()
    {
        if (progressData.savedImages.Count == 0 && HasSaveFile())
        {
            LoadProgress();
        }
        
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
        
        if (textureCache.ContainsKey(imageId) && textureCache[imageId] != null)
        {
            Destroy(textureCache[imageId]);
            textureCache.Remove(imageId);
        }
        
        ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
        
        if (progress != null && !string.IsNullOrEmpty(progress.savedPath) && File.Exists(progress.savedPath))
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(progress.savedPath);
                
                // Luôn tạo texture mới
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = $"Texture_{imageId}"; // Đặt tên cho texture để dễ debug
                
                if (texture.LoadImage(fileData))
                {
                    
                    // Kiểm tra pixel để đảm bảo texture có dữ liệu
                    try {
                        Color topLeftPixel = texture.GetPixel(0, 0);
                    }
                    catch (Exception ) {
                    }
                    
                    textureCache[imageId] = texture;
                    return texture;
                }
                else
                {
                    Destroy(texture);
                    return null;
                }
            }
            catch (System.Exception )
            {
            }
        }
        
        return null;
    }

    private string GetSavedImagePath(string imageId)
    {
        ColoringProgress progress = progressData.savedImages.Find(p => p.imageId == imageId);
        return progress != null ? progress.savedPath : null;
    }

    private void LoadProgress()
    {
        
        try
        {
            string json = File.ReadAllText(saveDataPath);
            progressData = JsonUtility.FromJson<ProgressData>(json);
        }
        catch (Exception )
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
            
            OnProgressUpdated?.Invoke(progressData.savedImages);
        }
        catch (Exception )
        {
        }
    }

    public void PrepareForSceneChange()
    {
        SaveProgress();
    }
}