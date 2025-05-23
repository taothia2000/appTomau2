using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingToolButton : MonoBehaviour
{
    public ColoringManager coloringManager;
    public ColoringManager.DrawingMode drawingMode;
    public Sprite onSprite;
    public Sprite offSprite;
    private Button button;
    private Image buttonImage;
    private Texture2D drawCursor;   
    private Texture2D brushCursor; 

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        if (buttonImage != null && offSprite != null)
        {
            buttonImage.sprite = offSprite;
        }
    }

    private void HighlightSelectedButton()
    {
        DrawingToolButton[] allButtons = FindObjectsOfType<DrawingToolButton>();
        foreach (var btn in allButtons)
        {
            if (btn.buttonImage != null && btn.offSprite != null)
            {
                btn.buttonImage.sprite = btn.offSprite;
            }
        }

        if (buttonImage != null && onSprite != null)
        {
            buttonImage.sprite = onSprite;
        }
    }

    private void SetCursorForTool()
    {
        if (drawCursor == null || brushCursor == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Texture2D cursorToUse = null;
        Vector2 hotspot = Vector2.zero;

        switch (drawingMode)
        {
            case ColoringManager.DrawingMode.Draw:
                cursorToUse = drawCursor; 
                hotspot = drawCursor != null ? new Vector2(0, drawCursor.height) : Vector2.zero;
                break;
            case ColoringManager.DrawingMode.Brush:
                cursorToUse = brushCursor;
                hotspot = brushCursor != null ? new Vector2(brushCursor.width/2, brushCursor.height) : Vector2.zero;
                break;
        }

        if (cursorToUse != null && cursorToUse.isReadable)
        {
            Cursor.SetCursor(cursorToUse, hotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    public void OnButtonClick()
    {
        if (coloringManager != null)
        {
            coloringManager.SetDrawingMode(drawingMode);
            HighlightSelectedButton();
            SetCursorForTool();
        }
    }
}