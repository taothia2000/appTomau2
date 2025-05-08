using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public ColoringManager coloringManager;
    public Color buttonColor;
    public Color originalColor { get; private set; }
    private Button button;
    public ColorButtonCustomizer colorCustomizer;

    private bool isLongPressing = false;
    private float longPressThreshold = 0.5f;
    private Coroutine longPressCoroutine;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            originalColor = buttonColor;
            Debug.Log($"Button {gameObject.name} initialized with original color: {ColorUtility.ToHtmlStringRGB(originalColor)}");

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
            button.colors = colors;

            button.onClick.AddListener(OnColorButtonClick);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Pointer down on " + gameObject.name);
        isLongPressing = true;

        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
        }
        longPressCoroutine = StartCoroutine(LongPressDetection());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Pointer up on " + gameObject.name);
        isLongPressing = false;

        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private IEnumerator LongPressDetection()
    {
        float pressTime = 0;

        while (isLongPressing && pressTime < longPressThreshold)
        {
            pressTime += Time.deltaTime;
            yield return null;
        }

        if (isLongPressing)
        {
            Debug.Log("Long press detected on " + gameObject.name);
            OnLongPress();
        }
    }

    private void OnLongPress()
    {
        Debug.Log("Processing long press on " + gameObject.name);

        if (colorCustomizer != null)
        {
            Debug.Log("Showing color palette through customizer");
            colorCustomizer.ShowColorPalette(this);

            if (colorCustomizer.colorPalettePanel != null)
            {
                Debug.Log("Color palette panel active state after showing: " + 
                          colorCustomizer.colorPalettePanel.activeSelf);
            }
            else
            {
                Debug.LogError("Color palette panel is null after showing!");
            }
        }
        else
        {
            Debug.LogError("Cannot show color palette: ColorButtonCustomizer is null!");
        }
    }

    public void OnColorButtonClick()
    {
        if (coloringManager != null)
        {
            Color newColor = buttonColor;
            newColor.a = 1f;
            coloringManager.SetColor(newColor);

            HighlightSelectedButton();
        }
    }

    private void HighlightSelectedButton()
    {
        ColorButton[] allButtons = FindObjectsOfType<ColorButton>();
        foreach (var btn in allButtons)
        {
            if (btn.button != null)
            {
                ColorBlock colors = btn.button.colors;
                colors.normalColor = new Color(btn.buttonColor.r, btn.buttonColor.g, btn.buttonColor.b, 0.5f);
                btn.button.colors = colors;
            }
        }

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            button.colors = colors;
        }
    }
   public void ResetToOriginalColor()
    {
        buttonColor = originalColor;
        Debug.Log($"Button {gameObject.name} reset to original color: {ColorUtility.ToHtmlStringRGB(originalColor)}");

        // Cập nhật giao diện của nút
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
            button.colors = colors;
            button.enabled = true; // Đảm bảo nút không bị tắt
            button.gameObject.SetActive(true); // Đảm bảo GameObject không bị ẩn
        }

        if (coloringManager != null)
        {
            Color fullColor = buttonColor;
            fullColor.a = 1f;
            coloringManager.SetColor(fullColor);
        }

        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = buttonColor;
            buttonImage.enabled = true; // Đảm bảo Image không bị tắt
        }

        // Cập nhật Image của parent nếu có
        Transform parent = transform.parent;
        if (parent != null)
        {
            Image parentImage = parent.GetComponent<Image>();
            if (parentImage != null)
            {
                parentImage.color = buttonColor;
                parentImage.enabled = true;
            }
        }
    }
    public bool IsOriginalColor()
    {
        return buttonColor == originalColor;
    }
}