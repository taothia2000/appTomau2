using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class EventTriggerSetup : MonoBehaviour
{
    private ColorButton colorButton;
    private EventTrigger eventTrigger;
    private bool isLongPressing = false;
    private float longPressThreshold = 0.5f;
    private Coroutine longPressCoroutine;

    void Start()
    {
        colorButton = GetComponent<ColorButton>();
        if (colorButton == null)
        {
            Debug.LogError("EventTriggerSetup requires ColorButton component on the same GameObject!");
            return;
        }

        Debug.Log("Setting up EventTrigger for " + gameObject.name);

        eventTrigger = gameObject.AddComponent<EventTrigger>();
        eventTrigger.triggers.Clear();

        AddEventTrigger(EventTriggerType.PointerDown, OnPointerDown);
        AddEventTrigger(EventTriggerType.PointerUp, OnPointerUp);
        AddEventTrigger(EventTriggerType.PointerExit, OnPointerExit);
        AddEventTrigger(EventTriggerType.PointerClick, OnClick);
        // Xóa AddEventTrigger(EventTriggerType.Drag, OnDrag) để ScrollRect tự xử lý

        Debug.Log("EventTrigger setup completed for " + gameObject.name);
    }

    private void AddEventTrigger(EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback = new EventTrigger.TriggerEvent();
        entry.callback.AddListener(action);
        eventTrigger.triggers.Add(entry);
        Debug.Log($"Added {type} event to {gameObject.name}");
    }

    private void OnPointerDown(BaseEventData eventData)
    {
        Debug.Log("Pointer down on " + gameObject.name);
        isLongPressing = true;
        
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
        }
        longPressCoroutine = StartCoroutine(LongPressDetection());
    }

    private void OnPointerUp(BaseEventData eventData)
    {
        Debug.Log("Pointer up on " + gameObject.name);
        isLongPressing = false;
        
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private void OnPointerExit(BaseEventData eventData)
    {
        Debug.Log("Pointer exit on " + gameObject.name);
        isLongPressing = false;
        
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private void OnClick(BaseEventData eventData)
    {
        if (!isLongPressing && colorButton != null)
        {
            Debug.Log("Click detected on " + gameObject.name + " (not long press), calling OnColorButtonClick");
            colorButton.OnColorButtonClick();
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
        
        if (colorButton != null && colorButton.colorCustomizer != null)
        {
            Debug.Log("Showing color palette through customizer");
            colorButton.colorCustomizer.ShowColorPalette(colorButton);
            
            if (colorButton.colorCustomizer.colorPalettePanel != null)
            {
                Debug.Log("Color palette panel active state after showing: " + 
                          colorButton.colorCustomizer.colorPalettePanel.activeSelf);
            }
            else
            {
                Debug.LogError("Color palette panel is null after showing!");
            }
        }
        else
        {
            Debug.LogError("Cannot show color palette: ColorButton or ColorButtonCustomizer is null!");
        }
    }
}