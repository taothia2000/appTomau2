using System;
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
            if (buttonColor.a == 0f)
            {
                buttonColor.a = 1f;
                Debug.LogWarning($"Fixed buttonColor alpha = 0f for button {gameObject.name} in Start");
            }

            originalColor = buttonColor;
            Debug.Log($"Button {gameObject.name} initialized with original color: {ColorUtility.ToHtmlStringRGB(originalColor)}");

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
            button.colors = colors;
            Debug.Log($"Start: normalColor alpha = {colors.normalColor.a}, Image alpha = {GetComponent<Image>().color.a}");

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
            DebugUIState("After clicking color button");
            
            gameObject.SetActive(true);
        }
    }

    private void DebugUIState(string context)
    {
        Image buttonImage = GetComponent<Image>();
        Button btn = GetComponent<Button>();
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        Transform parent = transform.parent;
        Image parentImage = parent != null ? parent.GetComponent<Image>() : null;

        Debug.Log($"[{context}] Button: {gameObject.name}");
        Debug.Log($"Image - Color: {buttonImage.color}, Enabled: {buttonImage.enabled}, Active: {buttonImage.gameObject.activeSelf}");
        Debug.Log($"Button - Enabled: {btn.enabled}, Active: {btn.gameObject.activeSelf}");
        if (canvasGroup != null)
        {
            Debug.Log($"CanvasGroup - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}, BlocksRaycasts: {canvasGroup.blocksRaycasts}");
        }
        else
        {
            Debug.Log("CanvasGroup: Not present");
        }
        if (parentImage != null)
        {
            Debug.Log($"Parent Image - Color: {parentImage.color}, Enabled: {parentImage.enabled}, Active: {parentImage.gameObject.activeSelf}");
        }
        else
        {
            Debug.Log("Parent Image: Not present");
        }
    }

    private void HighlightSelectedButton()
    {
        ColorButton[] allButtons = FindObjectsOfType<ColorButton>();
        
        foreach (var btn in allButtons)
        {
            if (btn == null || btn.button == null)
                continue;
                
            ColorBlock colors = btn.button.colors;
            colors.normalColor = new Color(btn.buttonColor.r, btn.buttonColor.g, btn.buttonColor.b, 0.5f);
            btn.button.colors = colors;
            
            btn.gameObject.SetActive(true);
            
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null && btnImage.color.a == 0f)
            {
                btnImage.color = new Color(btnImage.color.r, btnImage.color.g, btnImage.color.b, 1f);
                Debug.LogWarning($"Fixed Image alpha = 0f for button {btn.gameObject.name}");
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
        if (buttonColor.a == 0f)
        {
            buttonColor.a = 1f;
            Debug.LogWarning($"Fixed buttonColor alpha = 0f for button {gameObject.name} in ResetToOriginalColor");
        }

        buttonColor = originalColor;
        Debug.Log($"Button {gameObject.name} reset to original color: {ColorUtility.ToHtmlStringRGB(originalColor)}");

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
            button.colors = colors;
            button.enabled = true;
            Debug.Log($"ResetToOriginalColor: normalColor alpha = {colors.normalColor.a}");
        }
        
        gameObject.SetActive(true);

        if (coloringManager != null)
        {
            Color fullColor = buttonColor;
            fullColor.a = 1f;
            coloringManager.SetColor(fullColor);
        }

        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 1f);
            buttonImage.enabled = true;
            Debug.Log($"ResetToOriginalColor: Image alpha = {buttonImage.color.a}");
        }

        Transform parent = transform.parent;
        if (parent != null)
        {
            Image parentImage = parent.GetComponent<Image>();
            if (parentImage != null)
            {
                parentImage.color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 1f);
                parentImage.enabled = true;
                Debug.Log($"ResetToOriginalColor: Parent Image alpha = {parentImage.color.a}");
            }
        }
    }
    
    public bool IsOriginalColor()
    {
        return buttonColor == originalColor;
    }
    
    public void ForceShow()
    {
        gameObject.SetActive(true);
        
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.enabled = true;
            if (img.color.a == 0f)
            {
                img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
                Debug.LogWarning($"Fixed Image alpha = 0f in ForceShow for button {gameObject.name}");
            }
        }
        
        if (button != null) button.enabled = true;
        
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    void LateUpdate()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null && canvasGroup.alpha == 0f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.LogWarning($"Fixed CanvasGroup alpha = 0f for button {gameObject.name}");
        }

        CanvasGroup parentCanvasGroup = transform.parent != null ? transform.parent.GetComponent<CanvasGroup>() : null;
        if (parentCanvasGroup != null && parentCanvasGroup.alpha == 0f)
        {
            parentCanvasGroup.alpha = 1f;
            parentCanvasGroup.interactable = true;
            parentCanvasGroup.blocksRaycasts = true;
            Debug.LogWarning($"Fixed Parent CanvasGroup alpha = 0f for button {gameObject.name}");
        }

        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null && buttonImage.color.a == 0f)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
            Debug.LogWarning($"Fixed Image alpha = 0f for button {gameObject.name}");
        }
    }
}