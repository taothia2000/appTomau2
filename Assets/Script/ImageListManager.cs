using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ImageListManager : MonoBehaviour
{
    public SaveManager saveManager;
    public GameObject imageItemPrefab;
    public Transform contentContainer;
    
    [SerializeField] private string coloringSceneName = "Coloring"; // Tên scene tô màu
    
    private List<GameObject> instantiatedItems = new List<GameObject>();
    
    void Start()
    {
        if (saveManager != null)
        {
            // Đăng ký lắng nghe sự kiện cập nhật
            saveManager.OnProgressUpdated += UpdateImageList;
            
            // Hiển thị danh sách ban đầu
            UpdateImageList(saveManager.GetAllProgress());
        }
    }
    
    void OnDestroy()
    {
        if (saveManager != null)
        {
            saveManager.OnProgressUpdated -= UpdateImageList;
        }
    }
    
    public void UpdateImageList(List<SaveManager.ColoringProgress> progressList)
    {
        // Xóa các item cũ
        foreach (var item in instantiatedItems)
        {
            Destroy(item);
        }
        instantiatedItems.Clear();
        
        // Tạo các item mới
        foreach (var progress in progressList)
        {
            GameObject newItem = Instantiate(imageItemPrefab, contentContainer);
            instantiatedItems.Add(newItem);
            
            // Cấu hình item
            ImageListItem itemScript = newItem.GetComponent<ImageListItem>();
            if (itemScript != null)
            {
                // Tải ảnh đã lưu
                Texture2D savedTexture = saveManager.LoadSavedImage(progress.imageId);
                
                // Cấu hình item
                itemScript.Setup(
                    progress.imageId, 
                    savedTexture, 
                    progress.lastSaved.ToString("dd/MM/yyyy HH:mm"), 
                    progress.isCompleted
                );
                
                // Thêm sự kiện khi nhấp vào
                itemScript.OnItemClicked += HandleItemClicked;
            }
        }
    }
    
    private void HandleItemClicked(string imageId)
    {
        // Xử lý khi ảnh được chọn (mở để tiếp tục tô)
        Debug.Log($"Chọn ảnh có ID: {imageId}");
        
        // Lưu ID ảnh đã chọn vào PlayerPrefs để truyền giữa các scene
        PlayerPrefs.SetString("SelectedImageId", imageId);
        PlayerPrefs.Save();
        
        // Chuyển đến scene tô màu
        if (SceneExists(coloringSceneName))
        {
            SceneManager.LoadScene(coloringSceneName);
        }
        else
        {
            Debug.LogError($"Không tìm thấy scene {coloringSceneName} trong build settings!");
        }
    }
    
    private bool SceneExists(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string scnName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (scnName == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}