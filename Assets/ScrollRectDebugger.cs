using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectDebugger : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("ScrollRect: OnBeginDrag called");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("ScrollRect: OnDrag called");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("ScrollRect: OnEndDrag called");
    }
}