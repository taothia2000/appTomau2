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
    public Button button;
    public ColorButtonCustomizer colorCustomizer;

    private bool isLongPressing = false;
    private float longPressThreshold = 0.5f;
    private Coroutine longPressCoroutine;

    // Thêm Outline component để tạo viền sáng
    private Outline outline;

    void Start()
    {
        button = GetComponent<Button>();
        outline = gameObject.AddComponent<Outline>(); // Thêm Outline nếu chưa có
        outline.effectColor = Color.white; // Màu viền sáng
        outline.effectDistance = new Vector2(4f, -4f); // Độ dày viền
        outline.enabled = false; // Ban đầu tắt viền

        if (button != null)
        {
            if (buttonColor.a == 0f)
            {
                buttonColor.a = 1f;
            }

            originalColor = buttonColor;

            // Sử dụng màu gốc đầy đủ, không làm mờ
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor; // Màu gốc với alpha = 1
            button.colors = colors;

            button.onClick.AddListener(OnColorButtonClick);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isLongPressing = true;

        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
        }
        longPressCoroutine = StartCoroutine(LongPressDetection());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
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
            OnLongPress();
        }
    }

    private void OnLongPress()
    {
        if (colorCustomizer != null)
        {
            colorCustomizer.ShowColorPalette(this);
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
            
            gameObject.SetActive(true);
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
        colors.normalColor = btn.buttonColor; // Giữ màu gốc, không làm mờ
        btn.button.colors = colors;
        
        btn.gameObject.SetActive(true);
        
        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null && btnImage.color.a != 1f)
        {
            btnImage.color = new Color(btnImage.color.r, btnImage.color.g, btnImage.color.b, 1f);
        }

        if (btn.outline != null && btn != this) // Tắt viền cho các button khác
        {
            btn.outline.enabled = false;
        }
    }

    if (button != null)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor; // Giữ màu gốc
        button.colors = colors;

        if (outline != null)
        {
            outline.enabled = true; // Bật viền cho button được chọn
        }
    }
}

    public void ResetToOriginalColor()
    {
        if (buttonColor.a == 0f)
        {
            buttonColor.a = 1f;
        }

        buttonColor = originalColor;

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor; // Sử dụng màu gốc
            button.colors = colors;
            button.enabled = true;
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
        }

        Transform parent = transform.parent;
        if (parent != null)
        {
            Image parentImage = parent.GetComponent<Image>();
            if (parentImage != null)
            {
                parentImage.color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 1f);
                parentImage.enabled = true;
            }
        }

        // Tắt viền khi reset
        if (outline != null)
        {
            outline.enabled = false;
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

        // Đảm bảo viền tắt khi ForceShow
        if (outline != null)
        {
            outline.enabled = false;
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
        }

        CanvasGroup parentCanvasGroup = transform.parent != null ? transform.parent.GetComponent<CanvasGroup>() : null;
        if (parentCanvasGroup != null && parentCanvasGroup.alpha == 0f)
        {
            parentCanvasGroup.alpha = 1f;
            parentCanvasGroup.interactable = true;
            parentCanvasGroup.blocksRaycasts = true;
        }

        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null && buttonImage.color.a == 0f)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
        }
    }
}