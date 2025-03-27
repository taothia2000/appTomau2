using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    public static ConfirmPopup Instance;
    public GameObject popup;
    public Button confirmButton;
    public Button cancelButton;
    private System.Action<bool> callback;
    public Text confirmText;

    void Awake()
    {
        Instance = this;
        popup.SetActive(false);
    }
    public static void Show(string message, System.Action<bool> onConfirm)
    {
        Instance.popup.SetActive(true);
        Instance.confirmText.text = message;
        Instance.callback = onConfirm;
    }
    public void OnConfirm()
    {
        callback?.Invoke(true);
        popup.SetActive(false);
    }

    public void OnCancel()
    {
        callback?.Invoke(false);
        popup.SetActive(false);
    }
}