using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public ColoringManager coloringManager;
    public Color buttonColor;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
            button.colors = colors;
        }
    }

    public void OnColorButtonClick()
    {
        if (coloringManager != null)
        {
            Color newColor = buttonColor;
            newColor.a = 1f;
            coloringManager.SetColor(newColor);

            // Highlight nút màu được chọn
            HighlightSelectedButton();
        }
    }

    private void HighlightSelectedButton()
    {
        // Reset tất cả các nút màu về trạng thái mờ
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

        // Highlight nút được chọn
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            button.colors = colors;
        }
    }
}