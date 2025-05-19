using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSelectManager : MonoBehaviour
{
    public GameObject imageSelectButtonPrefab;
    public Transform parentTransform;
    public List<ImageData> imageList;

    [System.Serializable]
    public class ImageData
    {
        public string id;
        public Texture2D texture;
        public string lastSavedTime;
    }

    private void Start()
    {
        InputField sceneInput = GameObject.FindWithTag("SceneNameInput")?.GetComponent<InputField>();
        if (sceneInput == null)
        {
            Debug.LogError("Không tìm thấy InputField với tag 'SceneNameInput'!");
            return;
        }
Debug.Log("Found InputField: " + sceneInput.name + ", value: " + sceneInput.text);
        foreach (var imageData in imageList)
        {
            GameObject buttonObj = Instantiate(imageSelectButtonPrefab, parentTransform);
            ImageSelectButton button = buttonObj.GetComponent<ImageSelectButton>();
            Debug.Log("Instantiated button: " + buttonObj.name + ", sceneNameInput: " + (button.sceneNameInput != null ? button.sceneNameInput.name : "null"));
            button.sceneNameInput = sceneInput; // Gán InputField chính xác
            button.Setup(imageData.id, imageData.texture, imageData.lastSavedTime);
        }
    }
}