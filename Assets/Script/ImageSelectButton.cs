using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class ImageSelectButton : MonoBehaviour
{
    public Image thumbnailImage;
    public Text lastSavedText;
    public InputField sceneNameInput;

    public string imageId;
    private SaveManager saveManager;
    
    private void Start()
    {
        PlayerPrefs.DeleteKey("SelectedScene");
        PlayerPrefs.Save();
        Debug.Log("Reset PlayerPrefs.SelectedScene");
        saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            saveManager = SaveManager.Instance;
        }
        if (sceneNameInput == null)
        {
            GameObject inputFieldObj = GameObject.FindWithTag("SceneNameInput");
            if (inputFieldObj != null)
            {
                sceneNameInput = inputFieldObj.GetComponent<InputField>();
                Debug.Log("Assigned sceneNameInput in Start: " + (sceneNameInput != null ? sceneNameInput.name : "null"));
            }
            else
            {
                Debug.LogWarning("No GameObject found with tag 'SceneNameInput'!");
            }
        }
        Debug.Log("InputField Value: " + (sceneNameInput != null ? sceneNameInput.text : "null") + ", isInteractable: " + (sceneNameInput != null ? sceneNameInput.interactable : "null"));
    }

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
            button.interactable = true;
            Debug.Log("Button OnClick listener added for: " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("No Button component found on: " + gameObject.name);
        }
    }
    
    public void Setup(string id, Texture2D texture, string lastSavedTime)
    {
        Debug.Log($"Setup called with id: {id}, texture: {(texture != null ? "not null" : "null")}, lastSavedTime: {lastSavedTime}");
        if (string.IsNullOrEmpty(id)) return;

        imageId = id;
        if (thumbnailImage == null) return;

        if (texture != null)
        {
            if (thumbnailImage.sprite != null) Destroy(thumbnailImage.sprite);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            thumbnailImage.sprite = sprite;
            thumbnailImage.preserveAspect = true;
        }

        if (lastSavedText != null) lastSavedText.text = "Lưu lần cuối: " + lastSavedTime;

        gameObject.name = "ImageButton_" + id;

        Button button = GetComponent<Button>();
        if (button != null)
        {
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                button.onClick.AddListener(OnButtonClick);
                Debug.Log("Setup - Added OnClick listener for: " + gameObject.name);
            }
        }
    }

    public void OnButtonClick()
    {
        Debug.Log("OnButtonClick called for: " + gameObject.name);
        try
        {
            if (sceneNameInput == null)
            {
                Debug.LogWarning("SceneNameInput is not assigned!");
                GameObject inputFieldObj = GameObject.FindWithTag("SceneNameInput");
                if (inputFieldObj != null)
                {
                    sceneNameInput = inputFieldObj.GetComponent<InputField>();
                    Debug.Log("Re-assigned sceneNameInput: " + (sceneNameInput != null ? sceneNameInput.name : "null"));
                }
                else
                {
                    Debug.LogWarning("No GameObject found with tag 'SceneNameInput'!");
                }
            }
            Debug.Log("InputField Value: " + (sceneNameInput != null ? sceneNameInput.text : "null"));

            if (string.IsNullOrEmpty(imageId))
            {
                Debug.LogWarning("imageId is null or empty!");
                return;
            }
            Debug.Log("imageId is valid: " + imageId);

            PlayerPrefs.SetString("SelectedImageId", imageId);
            Debug.Log("SelectedImageId saved to PlayerPrefs: " + imageId);

            string sceneName = (sceneNameInput != null && !string.IsNullOrEmpty(sceneNameInput.text)) 
                ? sceneNameInput.text.Trim().Replace(" ", "")
                : SceneManager.GetActiveScene().name;
            Debug.Log("Computed sceneName: " + sceneName);
            PlayerPrefs.SetString("SelectedScene", sceneName);
            PlayerPrefs.Save();
            Debug.Log("Saved SelectedScene to PlayerPrefs: " + sceneName);

            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            StartCoroutine(SaveImageAndLoadScene());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in OnButtonClick: " + ex.Message + "\nStackTrace: " + ex.StackTrace);
        }
    }

    private IEnumerator SaveImageAndLoadScene()
    {
        if (saveManager == null)
        {
            saveManager = FindObjectOfType<SaveManager>();
        }
        
        if (saveManager != null)
        {
            Texture2D existingTexture = saveManager.LoadSavedImage(imageId);
            
            if (existingTexture == null && thumbnailImage != null && thumbnailImage.sprite != null)
            {
                try {
                    Texture2D thumbnailTexture = null;
                    Texture2D resizedTexture = null;
                    try {
                        thumbnailTexture = new Texture2D(
                            (int)thumbnailImage.sprite.rect.width,
                            (int)thumbnailImage.sprite.rect.height,
                            TextureFormat.RGBA32,
                            false
                        );
                        
                        Color[] pixels = thumbnailImage.sprite.texture.GetPixels(
                            (int)thumbnailImage.sprite.textureRect.x,
                            (int)thumbnailImage.sprite.textureRect.y,
                            (int)thumbnailImage.sprite.textureRect.width,
                            (int)thumbnailImage.sprite.textureRect.height
                        );
                        
                        thumbnailTexture.SetPixels(pixels);
                        thumbnailTexture.Apply();

                        // Resize to 1000x1000
                        resizedTexture = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);
                        float aspectRatio = (float)thumbnailTexture.width / thumbnailTexture.height;
                        int newWidth, newHeight;
                        int offsetX = 0, offsetY = 0;

                        if (aspectRatio > 1) // Wider than tall
                        {
                            newWidth = 1000;
                            newHeight = Mathf.RoundToInt(1000 / aspectRatio);
                            offsetY = (1000 - newHeight) / 2;
                        }
                        else // Taller than wide or square
                        {
                            newHeight = 1000;
                            newWidth = Mathf.RoundToInt(1000 * aspectRatio);
                            offsetX = (1000 - newWidth) / 2;
                        }

                        Color[] resizedPixels = new Color[1000 * 1000];
                        for (int i = 0; i < resizedPixels.Length; i++)
                        {
                            resizedPixels[i] = Color.clear; // Transparent background
                        }

                        for (int y = 0; y < newHeight; y++)
                        {
                            for (int x = 0; x < newWidth; x++)
                            {
                                int srcX = Mathf.FloorToInt((float)x / newWidth * thumbnailTexture.width);
                                int srcY = Mathf.FloorToInt((float)y / newHeight * thumbnailTexture.height);
                                resizedPixels[(y + offsetY) * 1000 + (x + offsetX)] = 
                                    thumbnailTexture.GetPixel(srcX, srcY);
                            }
                        }

                        resizedTexture.SetPixels(resizedPixels);
                        resizedTexture.Apply();
                        
                        saveManager.SaveImage(imageId, resizedTexture);
                    }
                    finally {
                        if (thumbnailTexture != null) {
                            Destroy(thumbnailTexture);
                        }
                        if (resizedTexture != null) {
                            Destroy(resizedTexture);
                        }
                    }
                }
                catch (Exception ex) {
                    Debug.LogError($"Error resizing and saving image: {ex.Message}");
                }
            }
        }
        
        yield return null;
        
        SceneManager.LoadScene("Main");
    }
    
    public string LastSavedTime
    {
        get { return lastSavedText != null ? lastSavedText.text.Replace("Last saved: ", "") : ""; }
    }

    private void OnEnable()
    {
        if (thumbnailImage == null)
        {
            thumbnailImage = GetComponentInChildren<Image>();
        }

        Button button = GetComponent<Button>();
        if (button != null)
        {
            int listenerCount = button.onClick.GetPersistentEventCount();
            Debug.Log("OnEnable - Button listener count for: " + gameObject.name + ": " + listenerCount);
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                button.onClick.AddListener(OnButtonClick);
                Debug.Log("OnEnable - Re-added OnClick listener for: " + gameObject.name);
            }
        }
    }

    public void OnPointerDownDebug()
    {
        Debug.Log("PointerDown on: " + gameObject.name);
    }
}