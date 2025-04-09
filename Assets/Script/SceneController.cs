using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadMainScene(string imageId)
    {
        StartCoroutine(LoadMainSceneRoutine(imageId));
    }

    private IEnumerator LoadMainSceneRoutine(string imageId)
    {
        // Lưu thông tin cần thiết trước khi chuyển scene
        PlayerPrefs.SetString("SelectedImageId", imageId);
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
