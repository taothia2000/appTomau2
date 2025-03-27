using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BackButton : MonoBehaviour
{
    private static Stack<string> sceneHistory = new Stack<string>();

    void Awake()
    {
        //DontDestroyOnLoad(gameObject);
    }

    public void LoadNewScene(string sceneName)
    {
        sceneHistory.Push(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(sceneName);
    }

    public void GoBack()
    {
        if (sceneHistory.Count > 0)
        {
            string previousScene = sceneHistory.Pop();
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.Log("Không có scene trước đó!");
        }
    }
}