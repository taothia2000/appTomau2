using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveButton : MonoBehaviour
{
    [SerializeField] private ColoringManager coloringManager;
    [SerializeField] private string stageSceneName = "Stage";

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
        if (ColoringManager.Instance != null && ColoringManager.Instance.saveManager != null)
        {
            // Lưu trạng thái hiện tại
            bool saveSuccess = ColoringManager.Instance.SaveCurrentImage();
            
            if (saveSuccess)
            {
                // Đảm bảo SaveManager được giữ lại qua scene
                var saveManager = ColoringManager.Instance.saveManager;
                if (saveManager.transform.parent != null)
            {
                saveManager.transform.parent = null; // Tách khỏi cha nếu có
            }
            DontDestroyOnLoad(saveManager.gameObject);
                
                // Đảm bảo progress được cập nhật
                SaveManager.Instance.SaveProgress();
                
                // Chuyển scene
                SceneManager.LoadScene(stageSceneName);
            }
        }
        else
        {
            Debug.LogError("ColoringManager hoặc SaveManager không tồn tại!");
        }
    }
}