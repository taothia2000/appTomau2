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
        Debug.Log("Save button clicked");
        if (ColoringManager.Instance != null)
        {
            // Save current state
            bool saveSuccess = ColoringManager.Instance.SaveCurrentImage();
            
            if (saveSuccess)
            {
                // Make sure progress is saved
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.SaveProgress();
                }
                
                // Return to stage selection scene
                SceneManager.LoadScene("Test");
            }
        }
        else
        {
            Debug.LogError("ColoringManager not found!");
        }
    }
}