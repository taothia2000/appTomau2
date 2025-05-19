using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveButton : MonoBehaviour
{
    [SerializeField] private ColoringManager coloringManager;

    private void Start()
    {
        if (coloringManager == null)
        {
            coloringManager = FindObjectOfType<ColoringManager>();
        }
        
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnSaveButtonClick);
        }
    }

public void OnSaveButtonClick()
{
    if (ColoringManager.Instance != null)
    {
        bool saveSuccess = ColoringManager.Instance.SaveCurrentImage();
        
        if (saveSuccess)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveProgress();
            }
            
            string targetScene = PlayerPrefs.GetString("SelectedScene", "Test");
            Debug.Log("Attempting to load scene from PlayerPrefs: " + targetScene);
            
            // Debug danh sách scene
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                Debug.Log($"Scene at index {i}: {sceneName}");
            }

            int sceneIndex = -1;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneName == targetScene)
                {
                    sceneIndex = i;
                    break;
                }
            }

            if (sceneIndex != -1)
            {
                Debug.Log($"Loading scene at index {sceneIndex}: {targetScene}");
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                Debug.LogWarning($"Scene {targetScene} not found in Build Settings. Loading default scene 'Test'.");
                SceneManager.LoadScene("Test");
            }
        }
        else
        {
            Debug.LogWarning("Save failed. Not loading new scene.");
        }
    }
    else
    {
        Debug.LogWarning("ColoringManager.Instance is null.");
    }
}
}