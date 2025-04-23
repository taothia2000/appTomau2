using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IndexedGridLayout : LayoutGroup
{
    public Vector2 cellSize = new Vector2(100, 100);
    public Vector2 spacing = new Vector2(10, 10);
    public int columns = 3;
    public Dictionary<int, RectTransform> indexedItems = new Dictionary<int, RectTransform>();

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        
        float width = rectTransform.rect.width;
        float cellWidth = cellSize.x + spacing.x;
        columns = Mathf.Max(1, Mathf.FloorToInt((width + spacing.x) / cellWidth));
    }

    public override void CalculateLayoutInputVertical()
    {
        // Calculate based on the items we have with indices
    }

    public override void SetLayoutHorizontal()
    {
        SetCellsPosition();
    }

    public override void SetLayoutVertical()
    {
        SetCellsPosition();
    }

    private void SetCellsPosition()
    {
        float width = rectTransform.rect.width;
        float cellWidth = cellSize.x + spacing.x;
        columns = Mathf.Max(1, Mathf.FloorToInt((width + spacing.x) / cellWidth));

        foreach (var pair in indexedItems)
        {
            int index = pair.Key - 1; // Convert from 1-based to 0-based
            if (index < 0) index = 0;
            
            int row = index / columns;
            int col = index % columns;

            float xPos = col * (cellSize.x + spacing.x);
            float yPos = row * (cellSize.y + spacing.y);

            RectTransform rectTrans = pair.Value;
            if (rectTrans != null)
            {
                rectTrans.sizeDelta = cellSize;
                rectTrans.anchoredPosition = new Vector2(xPos, -yPos);
            }
        }
    }

    public void AddItemAtIndex(int index, RectTransform item)
    {
        if (item != null)
        {
            item.SetParent(transform, false);
            indexedItems[index] = item;
            SetDirty();
        }
    }

    public void RemoveItem(int index)
    {
        if (indexedItems.ContainsKey(index))
        {
            indexedItems.Remove(index);
            SetDirty();
        }
    }

    public void ClearItems()
    {
        indexedItems.Clear();
        SetDirty();
    }
}