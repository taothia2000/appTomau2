using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SaveNotificationPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button okButton;
    [SerializeField] private Button viewProgressButton;
    [SerializeField] private float autoHideDelay = 3f; // Thời gian tự động tắt thông báo
    
    private static SaveNotificationPopup _instance;
    public static SaveNotificationPopup Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveNotificationPopup>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
        
        // Ẩn panel khi khởi tạo
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        // Thiết lập các button event
        if (okButton != null)
        {
            okButton.onClick.AddListener(HidePopup);
        }
        
        if (viewProgressButton != null)
        {
            viewProgressButton.onClick.AddListener(NavigateToStageScene);
        }
    }
    
    // Hiển thị thông báo lưu thành công
    public void ShowSaveSuccess()
    {
        if (messageText != null)
        {
            messageText.text = "Đã lưu thành công bức ảnh của bạn!";
        }
        
        ShowPopup();
        
        // Tự động ẩn sau một khoảng thời gian
        StartCoroutine(AutoHidePopup());
    }
    
    // Hiển thị thông báo với nội dung tùy chỉnh
    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        ShowPopup();
        
        // Tự động ẩn sau một khoảng thời gian
        StartCoroutine(AutoHidePopup());
    }
    
    private void ShowPopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            
            // Animation hiển thị nếu cần
            CanvasGroup canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn(canvasGroup, 0.3f));
            }
        }
    }
    
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            // Animation ẩn nếu cần
            CanvasGroup canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadeOut(canvasGroup, 0.3f));
            }
            else
            {
                popupPanel.SetActive(false);
            }
        }
    }
    
    private IEnumerator AutoHidePopup()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HidePopup();
    }
    
    private IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut(CanvasGroup canvasGroup, float duration)
    {
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        popupPanel.SetActive(false);
    }
    
    // Chuyển đến scene Stage để xem tiến độ
    private void NavigateToStageScene()
    {
        string stageSceneName = "Stage"; // Tên scene stage
        
        HidePopup();
        
        // Kiểm tra xem scene có tồn tại trong build settings không
        if (SceneExists(stageSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(stageSceneName);
        }
        else
        {
            Debug.LogError($"Không tìm thấy scene {stageSceneName} trong build settings!");
        }
    }
    
    private bool SceneExists(string sceneName)
    {
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string scnName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (scnName == sceneName)
            {
                return true;
            }
        }
        return false;
    }
}