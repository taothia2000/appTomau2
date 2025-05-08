using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorButtonCustomizer : MonoBehaviour
{
    public GameObject colorPalettePanel;
    private ColorButton currentEditingButton;
    private int numberOfColors = 36;
    private float radius = 100f;
    private float buttonSize = 50f;
    private InputField colorInputField;
    private Color selectedColor;
    private Color defaultColor;
    private RawImage previewCanvas;
    private Texture2D previewTexture;
    private GameObject colorSelector;
    private bool isDraggingSelector = false;
    private List<Image> colorImages;
    private CanvasGroup paletteCanvasGroup;
    private CanvasGroup canvasGroup;
    private Slider brightnessSlider;
    private Text brightnessLabelText;
    private Button resetButton;
    private RectTransform selectorRect;
    private List<Color> allColors; // Danh sách màu động, bao gồm cả màu mặc định

    void Start()
    {
        if (colorPalettePanel != null)
        {
            colorPalettePanel.SetActive(false);
            paletteCanvasGroup = colorPalettePanel.GetComponent<CanvasGroup>();
            if (paletteCanvasGroup == null)
            {
                paletteCanvasGroup = colorPalettePanel.AddComponent<CanvasGroup>();
            }
        }
        canvasGroup = GameObject.Find("Bt color").GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = GameObject.Find("Bt color").AddComponent<CanvasGroup>();
        }
        allColors = new List<Color>(); // Khởi tạo danh sách màu
    }

   public void ShowColorPalette(ColorButton button)
{
    if (button == null || colorPalettePanel == null)
    {
        Debug.LogError("Cannot show color palette: Button or ColorPalettePanel is null!");
        return;
    }

    currentEditingButton = button;

    if (currentEditingButton.buttonColor != null)
    {
        defaultColor = currentEditingButton.buttonColor;
        selectedColor = defaultColor; // Gán giá trị ban đầu
        selectedColor.a = 1f; // Đảm bảo alpha là 1
        Debug.Log($"Button color retrieved: {ColorUtility.ToHtmlStringRGB(selectedColor)} with alpha: {selectedColor.a}");
    }
    else
    {
        Debug.LogWarning("Button color is null, defaulting to white.");
        defaultColor = Color.white;
        selectedColor = defaultColor;
        selectedColor.a = 1f; // Đảm bảo alpha là 1
    }

    colorPalettePanel.SetActive(true);
    colorPalettePanel.transform.SetAsLastSibling();

    if (paletteCanvasGroup != null)
    {
        paletteCanvasGroup.alpha = 1;
        paletteCanvasGroup.interactable = true;
        paletteCanvasGroup.blocksRaycasts = true;
    }

    if (canvasGroup != null)
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        Debug.Log("Blocked interaction with other buttons");
    }

    if (currentEditingButton.coloringManager != null)
    {
        currentEditingButton.coloringManager.enabled = false;
        Debug.Log("Disabled ColoringManager while palette is open");
    }

    // Tạo danh sách màu động
    allColors.Clear();
    for (int i = 0; i < numberOfColors; i++)
    {
        float hue = (float)i / numberOfColors;
        Color color = Color.HSVToRGB(hue, 1f, 1f);
        allColors.Add(color);
    }

    // Thêm màu mặc định nếu chưa có
    if (!allColors.Exists(c => ColorsApproximatelyEqual(c, defaultColor)))
    {
        allColors.Add(defaultColor);
        Debug.Log($"Added default color {ColorUtility.ToHtmlStringRGB(defaultColor)} to color list.");
    }

    // Thêm màu tùy chỉnh #FE4200 nếu chưa có
    Color customColor;
    if (ColorUtility.TryParseHtmlString("#FE4200", out customColor) && !allColors.Exists(c => ColorsApproximatelyEqual(c, customColor)))
    {
        allColors.Add(customColor);
        Debug.Log($"Added custom color #FE4200 to color list.");
    }

    Transform container = colorPalettePanel.transform;
    CreateColorWheel(container);

    // Cập nhật nháp ngay lập tức với selectedColor
    if (previewCanvas != null && previewTexture != null)
    {
        ClearPreviewCanvas(); // Xóa nháp cũ
        DrawOnPreviewCanvas(50, 50); // Vẽ một điểm mẫu ở giữa nháp
        Debug.Log($"Preview canvas updated with default color: {ColorUtility.ToHtmlStringRGB(selectedColor)} with alpha: {selectedColor.a}");
    }

    if (colorInputField != null)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(selectedColor);
        colorInputField.text = hexColor;
        colorInputField.textComponent.SetAllDirty();
        Debug.Log("Color input field updated with hex: " + hexColor);
    }

    if (brightnessSlider != null && colorImages != null)
    {
        int defaultIndex = colorImages.FindIndex(img => ColorsApproximatelyEqual(img.color, defaultColor));
        if (defaultIndex >= 0)
        {
            float sliderValue = (float)defaultIndex / (colorImages.Count - 1);
            brightnessSlider.value = sliderValue;
            UpdateColorSelectorPosition(sliderValue);
            Debug.Log($"Slider set to {sliderValue} for default color index: {defaultIndex}, selectedColor: {ColorUtility.ToHtmlStringRGB(selectedColor)}");
        }
        else
        {
            brightnessSlider.value = 0.5f;
            UpdateColorSelectorPosition(0.5f);
            Debug.Log("Default color not found in wheel, slider set to 0.5");
        }
    }

    UpdateResetButtonState();
}

   private void ResetColor()
{
    if (currentEditingButton == null)
    {
        Debug.LogWarning("Cannot reset color: Current editing button is null!");
        return;
    }

    selectedColor = currentEditingButton.originalColor;
    selectedColor.a = 1f; // Đảm bảo alpha là 1

    // Kiểm tra xem originalColor có trong danh sách màu không, nếu không thì thêm vào
    if (!allColors.Exists(c => ColorsApproximatelyEqual(c, selectedColor)))
    {
        allColors.Add(selectedColor);
        Debug.Log($"Added original color {ColorUtility.ToHtmlStringRGB(selectedColor)} to color list.");
        CreateColorWheel(colorPalettePanel.transform); // Tái tạo vòng tròn màu
    }

    int originalIndex = colorImages.FindIndex(img => ColorsApproximatelyEqual(img.color, selectedColor));
    if (originalIndex >= 0 && brightnessSlider != null)
    {
        float sliderValue = (float)originalIndex / (colorImages.Count - 1);
        brightnessSlider.value = sliderValue;
        UpdateColorSelectorPosition(sliderValue);
        Debug.Log($"Reset to original color index: {originalIndex}, slider value: {sliderValue}, selectedColor: {ColorUtility.ToHtmlStringRGB(selectedColor)}");
    }
    else
    {
        brightnessSlider.value = 0.5f;
        UpdateColorSelectorPosition(0.5f);
        Debug.LogWarning("Original color not found in wheel, slider set to 0.5");
    }

    UpdateButtonVisuals();

    if (colorInputField != null)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(selectedColor);
        colorInputField.text = hexColor;
        colorInputField.textComponent.SetAllDirty();
        Debug.Log("Color input field updated with hex after reset: " + hexColor);
    }

    UpdateResetButtonState();
}

    private void CreateColorWheel(Transform paletteContainer)
    {
        foreach (Transform child in paletteContainer)
        {
            if (child.name.StartsWith("ColorOption_") || child.name == "BrightnessSlider" || child.name == "ApplyButton" || child.name == "PreviewCanvas" || child.name == "ColorSelector" || child.name == "ColorInput" || child.name == "ResetButton" || child.name == "BrightnessLabel")
            {
                Destroy(child.gameObject);
            }
        }

        colorSelector = new GameObject("ColorSelector");
        colorSelector.transform.SetParent(paletteContainer, false);
        selectorRect = colorSelector.AddComponent<RectTransform>();
        selectorRect.sizeDelta = new Vector2(20, 20);
        Image selectorImage = colorSelector.AddComponent<Image>();
        selectorImage.raycastTarget = false;
        Sprite starSprite = Resources.Load<Sprite>("star");
        if (starSprite != null)
        {
            selectorImage.sprite = starSprite;
        }
        else
        {
            Debug.LogWarning("Star sprite not found! Using default image.");
        }
        selectorRect.anchoredPosition = Vector2.zero;

        colorImages = new List<Image>();
        for (int i = 0; i < allColors.Count; i++)
        {
            Color color = allColors[i];

            GameObject buttonObj = new GameObject("ColorOption_" + i);
            buttonObj.transform.SetParent(paletteContainer, false);

            float angle = (float)i / allColors.Count * 360f;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(buttonSize, buttonSize);

            Image image = buttonObj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            colorImages.Add(image);

            Button button = buttonObj.AddComponent<Button>();
            button.enabled = true;
            ColorBlock colorBlock = button.colors;
            colorBlock.highlightedColor = new Color(color.r, color.g, color.b, 0.8f);
            colorBlock.pressedColor = new Color(color.r, color.g, color.b, 0.6f);
            button.colors = colorBlock;

            Color capturedColor = color;
            button.onClick.AddListener(() => SelectColorFromPalette(capturedColor));

            EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDownEntry.callback.AddListener((data) => { isDraggingSelector = true; });
            trigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) =>
            {
                if (isDraggingSelector)
                {
                    PointerEventData pointerData = (PointerEventData)data;
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        paletteContainer.GetComponent<RectTransform>(),
                        pointerData.position,
                        pointerData.pressEventCamera,
                        out localPos
                    );
                    selectorRect.anchoredPosition = localPos;

                    Image nearestColor = FindNearestColor(localPos);
                    if (nearestColor != null)
                    {
                        selectorImage.color = nearestColor.color;
                        SelectColorFromPalette(nearestColor.color);
                    }
                }
            });
            trigger.triggers.Add(dragEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUpEntry.callback.AddListener((data) =>
            {
                isDraggingSelector = false;
                Vector2 localPos = selectorRect.anchoredPosition;
                Image nearestColor = FindNearestColor(localPos);
                if (nearestColor != null)
                {
                    SelectColorFromPalette(nearestColor.color);
                }
            });
            trigger.triggers.Add(pointerUpEntry);
        }

        GameObject previewCanvasObj = new GameObject("PreviewCanvas");
        previewCanvasObj.transform.SetParent(paletteContainer, false);
        RectTransform previewRect = previewCanvasObj.AddComponent<RectTransform>();
        previewRect.anchoredPosition = new Vector2(radius + 80, 0);
        previewRect.sizeDelta = new Vector2(100, 100);

        previewCanvas = previewCanvasObj.AddComponent<RawImage>();
        previewCanvas.raycastTarget = true;
        previewTexture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
        ClearPreviewCanvas();
        previewCanvas.texture = previewTexture;

        EventTrigger previewTrigger = previewCanvasObj.AddComponent<EventTrigger>();
        EventTrigger.Entry previewDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        previewDragEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = (PointerEventData)data;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                previewRect,
                pointerData.position,
                pointerData.pressEventCamera,
                out localPos
            );
            localPos += new Vector2(50, 50);
            DrawOnPreviewCanvas((int)localPos.x, (int)localPos.y);
        });
        previewTrigger.triggers.Add(previewDragEntry);

        GameObject inputObj = new GameObject("ColorInput");
        inputObj.transform.SetParent(paletteContainer, false);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchoredPosition = new Vector2(0, -radius - 50);
        inputRect.sizeDelta = new Vector2(100, 30);

        colorInputField = inputObj.AddComponent<InputField>();
        colorInputField.contentType = InputField.ContentType.Standard;
        colorInputField.onValueChanged.AddListener(OnColorInputChanged);

        GameObject inputBackground = new GameObject("Background");
        inputBackground.transform.SetParent(inputObj.transform, false);
        RectTransform backgroundRect = inputBackground.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        Image backgroundImage = inputBackground.AddComponent<Image>();
        backgroundImage.color = Color.gray;

        GameObject inputText = new GameObject("Text");
        inputText.transform.SetParent(inputObj.transform, false);
        RectTransform textRect = inputText.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text inputFieldText = inputText.AddComponent<Text>();
        inputFieldText.alignment = TextAnchor.MiddleCenter;
        Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (legacyFont != null)
        {
            inputFieldText.font = legacyFont;
        }
        colorInputField.textComponent = inputFieldText;

        GameObject sliderObj = new GameObject("BrightnessSlider");
        sliderObj.transform.SetParent(paletteContainer, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -radius - 90);
        sliderRect.sizeDelta = new Vector2(150, 20);

        brightnessSlider = sliderObj.AddComponent<Slider>();
        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.onValueChanged.AddListener(AdjustBrightness);

        GameObject sliderBackground = new GameObject("Background");
        sliderBackground.transform.SetParent(sliderObj.transform, false);
        RectTransform sliderBgRect = sliderBackground.AddComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.sizeDelta = Vector2.zero;
        Image sliderBgImage = sliderBackground.AddComponent<Image>();
        sliderBgImage.color = Color.gray;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.white;

        GameObject handleSlideArea = new GameObject("Handle Slide Area");
        handleSlideArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleSlideRect = handleSlideArea.AddComponent<RectTransform>();
        handleSlideRect.anchorMin = Vector2.zero;
        handleSlideRect.anchorMax = Vector2.one;
        handleSlideRect.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleSlideArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        brightnessSlider.fillRect = fillRect;
        brightnessSlider.handleRect = handleRect;

        GameObject labelObj = new GameObject("BrightnessLabel");
        labelObj.transform.SetParent(paletteContainer, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-100, -radius - 90);
        labelRect.sizeDelta = new Vector2(80, 20);

        brightnessLabelText = labelObj.AddComponent<Text>();
        brightnessLabelText.text = "Color Select";
        brightnessLabelText.color = Color.white;
        brightnessLabelText.alignment = TextAnchor.MiddleCenter;
        if (legacyFont != null)
        {
            brightnessLabelText.font = legacyFont;
        }

        GameObject applyButtonObj = new GameObject("ApplyButton");
        applyButtonObj.transform.SetParent(paletteContainer, false);
        RectTransform applyButtonRect = applyButtonObj.AddComponent<RectTransform>();
        applyButtonRect.anchoredPosition = new Vector2(-60, -radius - 120);
        applyButtonRect.sizeDelta = new Vector2(100, 30);

        Image applyBtnImage = applyButtonObj.AddComponent<Image>();
        applyBtnImage.color = new Color(0.2f, 0.6f, 0.2f);
        applyBtnImage.raycastTarget = true;

        Button applyBtn = applyButtonObj.AddComponent<Button>();
        applyBtn.enabled = true;
        applyBtn.onClick.AddListener(ApplySelectedColor);

        GameObject applyTextObj = new GameObject("Text");
        applyTextObj.transform.SetParent(applyButtonObj.transform, false);
        RectTransform applyTextRect = applyTextObj.AddComponent<RectTransform>();
        applyTextRect.anchorMin = Vector2.zero;
        applyTextRect.anchorMax = Vector2.one;
        applyTextRect.sizeDelta = Vector2.zero;

        Text applyButtonText = applyTextObj.AddComponent<Text>();
        applyButtonText.text = "Apply";
        applyButtonText.color = Color.white;
        applyButtonText.alignment = TextAnchor.MiddleCenter;
        if (legacyFont != null)
        {
            applyButtonText.font = legacyFont;
        }

        GameObject resetButtonObj = new GameObject("ResetButton");
        resetButtonObj.transform.SetParent(paletteContainer, false);
        RectTransform resetButtonRect = resetButtonObj.AddComponent<RectTransform>();
        resetButtonRect.anchoredPosition = new Vector2(60, -radius - 120);
        resetButtonRect.sizeDelta = new Vector2(100, 30);

        Image resetBtnImage = resetButtonObj.AddComponent<Image>();
        resetBtnImage.color = new Color(0.6f, 0.2f, 0.2f);
        resetBtnImage.raycastTarget = true;

        resetButton = resetButtonObj.AddComponent<Button>();
        resetButton.enabled = true;
        resetButton.onClick.AddListener(ResetColor);

        GameObject resetTextObj = new GameObject("Text");
        resetTextObj.transform.SetParent(resetButtonObj.transform, false);
        RectTransform resetTextRect = resetTextObj.AddComponent<RectTransform>();
        resetTextRect.anchorMin = Vector2.zero;
        resetTextRect.anchorMax = Vector2.one;
        resetTextRect.sizeDelta = Vector2.zero;

        Text resetButtonText = resetTextObj.AddComponent<Text>();
        resetButtonText.text = "Reset";
        resetButtonText.color = Color.white;
        resetButtonText.alignment = TextAnchor.MiddleCenter;
        if (legacyFont != null)
        {
            resetButtonText.font = legacyFont;
        }
    }

    public void SelectColorFromPalette(Color newColor)
    {
        if (currentEditingButton == null)
        {
            return;
        }

        selectedColor = newColor;
        int index = colorImages.FindIndex(img => ColorsApproximatelyEqual(img.color, newColor));
        if (index >= 0 && brightnessSlider != null)
        {
            float sliderValue = (float)index / (colorImages.Count - 1);
            brightnessSlider.value = sliderValue;
            UpdateColorSelectorPosition(sliderValue);
            Debug.Log($"Selected color index: {index}, slider value set to {sliderValue}");
        }

        UpdateButtonVisuals();
        UpdateResetButtonState();
    }

   private void UpdateColorSelectorPosition(float sliderValue)
{
    int colorIndex = Mathf.FloorToInt(sliderValue * (colorImages.Count - 1));
    colorIndex = Mathf.Clamp(colorIndex, 0, colorImages.Count - 1);
    float angle = (float)colorIndex / colorImages.Count * 360f;
    float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
    float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
    selectorRect.anchoredPosition = new Vector2(x, y);

    Image currentColor = colorImages[colorIndex];
    if (currentColor != null)
    {
        selectedColor = currentColor.color;
        selectedColor.a = 1f; // Đảm bảo alpha là 1
        colorSelector.GetComponent<Image>().color = selectedColor;
        UpdateButtonVisuals();
        UpdatePreviewCanvas(); // Cập nhật nháp
        Debug.Log($"Color selector updated to: {ColorUtility.ToHtmlStringRGB(selectedColor)} with alpha: {selectedColor.a} at index: {colorIndex}");
    }
}

private void AdjustBrightness(float brightness)
{
    UpdateColorSelectorPosition(brightness);

    if (colorInputField != null)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(selectedColor);
        colorInputField.text = hexColor;
        colorInputField.textComponent.SetAllDirty();
        Debug.Log($"Slider adjusted to {brightness}, new color: {hexColor}");
    }

    if (brightnessLabelText != null)
    {
        brightnessLabelText.text = $"Color Select: {brightness:F1}";
    }

    UpdatePreviewCanvas(); // Cập nhật nháp
    UpdateResetButtonState();
}
    private void UpdatePreviewCanvas()
{
    if (previewCanvas != null && previewTexture != null)
    {
        ClearPreviewCanvas();
        Color drawColor = selectedColor;
        drawColor.a = 1f; // Đảm bảo alpha là 1 khi vẽ
        Color tempColor = selectedColor; // Lưu giá trị gốc
        selectedColor = drawColor; // Sử dụng màu với alpha 1 để vẽ
        DrawOnPreviewCanvas(50, 50); // Vẽ lại nháp với selectedColor
        selectedColor = tempColor; // Khôi phục giá trị gốc của selectedColor
        Debug.Log($"Preview canvas updated with color: {ColorUtility.ToHtmlStringRGB(drawColor)} with alpha: {drawColor.a}");
    }
}

    public void ApplySelectedColor()
    {
        if (currentEditingButton == null)
        {
            return;
        }

        if (selectedColor == null)
        {
            selectedColor = defaultColor;
            Debug.Log("Using default color: " + ColorUtility.ToHtmlStringRGB(defaultColor));
        }

        Color appliedColor = selectedColor;
        appliedColor.a = 1f;
        currentEditingButton.buttonColor = appliedColor;
        UpdateButtonVisuals();

        if (currentEditingButton.coloringManager != null)
        {
            Color fullColor = appliedColor;
            fullColor.a = 1f;
            currentEditingButton.coloringManager.SetColor(fullColor);
            Debug.Log($"Applied color: {ColorUtility.ToHtmlStringRGB(fullColor)}");
        }

        HideColorPalette();
    }

    public void HideColorPalette()
    {
        if (colorPalettePanel != null)
        {
            colorPalettePanel.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("Restored interaction with other buttons");
            }

            ScrollRect scrollRect = colorPalettePanel.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.enabled = true;
                Debug.Log("ScrollRect enabled");
            }

            if (currentEditingButton.coloringManager != null)
            {
                currentEditingButton.coloringManager.enabled = true;
                Debug.Log("Enabled ColoringManager after closing palette");
            }

            if (previewTexture != null)
            {
                Destroy(previewTexture);
                previewTexture = null;
            }
        }
    }

    public void OnColorInputChanged(string hexColor)
    {
        if (ColorUtility.TryParseHtmlString("#" + hexColor, out Color newColor))
        {
            selectedColor = newColor;
            int index = colorImages.FindIndex(img => ColorsApproximatelyEqual(img.color, newColor));
            if (index >= 0 && brightnessSlider != null)
            {
                float sliderValue = (float)index / (colorImages.Count - 1);
                brightnessSlider.value = sliderValue;
                UpdateColorSelectorPosition(sliderValue);
                Debug.Log($"Color input changed to index: {index}, slider value set to {sliderValue}");
            }
            UpdateButtonVisuals();
            UpdateResetButtonState();
        }
    }

    private void UpdateButtonVisuals()
    {
        Button uiButton = currentEditingButton.GetComponent<Button>();
        if (uiButton != null)
        {
            ColorBlock colors = uiButton.colors;
            Color normalColor = selectedColor;
            normalColor.a = 0.5f;
            colors.normalColor = normalColor;
            uiButton.colors = colors;
            uiButton.enabled = true;
            uiButton.gameObject.SetActive(true);
        }

        Image buttonImage = currentEditingButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color imageColor = selectedColor;
            imageColor.a = 1f;
            buttonImage.color = imageColor;
            buttonImage.enabled = true;
        }

        Transform parent = currentEditingButton.transform.parent;
        if (parent != null)
        {
            Image parentImage = parent.GetComponent<Image>();
            if (parentImage != null)
            {
                Color parentColor = selectedColor;
                parentColor.a = 1f;
                parentImage.color = parentColor;
                parentImage.enabled = true;
            }
        }

        Debug.Log($"Updated button visuals with color: {ColorUtility.ToHtmlStringRGB(selectedColor)}");
    }

    private void UpdateResetButtonState()
    {
        if (resetButton != null && currentEditingButton != null)
        {
            bool isOriginal = ColorsApproximatelyEqual(selectedColor, currentEditingButton.originalColor);
            resetButton.interactable = !isOriginal;
            resetButton.GetComponent<Image>().color = isOriginal ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.6f, 0.2f, 0.2f);
            Debug.Log($"Reset button interactable: {resetButton.interactable}, selectedColor: {ColorUtility.ToHtmlStringRGB(selectedColor)}, originalColor: {ColorUtility.ToHtmlStringRGB(currentEditingButton.originalColor)}");
        }
    }

    private bool ColorsApproximatelyEqual(Color color1, Color color2, float tolerance = 0.001f)
    {
        return Mathf.Abs(color1.r - color2.r) < tolerance &&
               Mathf.Abs(color1.g - color2.g) < tolerance &&
               Mathf.Abs(color1.b - color2.b) < tolerance;
    }

    private void ClearPreviewCanvas()
    {
        Color[] colors = previewTexture.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        previewTexture.SetPixels(colors);
        previewTexture.Apply();
    }

    private void DrawOnPreviewCanvas(int x, int y)
    {
        if (x < 0 || x >= previewTexture.width || y < 0 || y >= previewTexture.height)
            return;

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < previewTexture.width && py >= 0 && py < previewTexture.height)
                {
                    previewTexture.SetPixel(px, py, selectedColor);
                }
            }
        }
        previewTexture.Apply();
    }

    private Image FindNearestColor(Vector2 position)
    {
        Image nearestColor = null;
        float minDistance = float.MaxValue;
        foreach (Image colorImage in colorImages)
        {
            RectTransform colorRect = colorImage.GetComponent<RectTransform>();
            Vector2 colorPos = colorRect.anchoredPosition;
            float distance = Vector2.Distance(position, colorPos);
            if (distance < minDistance && distance < buttonSize / 2)
            {
                minDistance = distance;
                nearestColor = colorImage;
            }
        }
        return nearestColor;
    }
}