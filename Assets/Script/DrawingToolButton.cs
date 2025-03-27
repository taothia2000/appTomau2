using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingToolButton : MonoBehaviour
{
    public ColoringManager coloringManager;
    public ColoringManager.DrawingMode drawingMode;
    private Button button;
    private Texture2D drawCursor;   
    private Texture2D brushCursor; 


    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = colors;
        }
    }

    private void HighlightSelectedButton()
    {
        // Reset tất cả các nút về trạng thái mờ
        DrawingToolButton[] allButtons = FindObjectsOfType<DrawingToolButton>();
        foreach (var btn in allButtons)
        {
            if (btn.button != null)
            {
                ColorBlock colors = btn.button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0.5f);
                btn.button.colors = colors;
            }
        }

        // Highlight nút được chọn
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            button.colors = colors;
        }
    }

   private void SetCursorForTool()
{
    // Thêm kiểm tra null trước khi sử dụng cursor
    if ( drawCursor == null || brushCursor == null )
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

    // Kiểm tra texture hợp lệ trước khi set
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