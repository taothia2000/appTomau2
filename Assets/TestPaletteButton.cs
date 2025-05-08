using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPaletteButton : MonoBehaviour
{
    public GameObject colorPalettePanel;
    
    public void OnClick()
    {
        if (colorPalettePanel != null)
        {
            bool newState = !colorPalettePanel.activeSelf;
            colorPalettePanel.SetActive(newState);
            Debug.Log("Setting palette active: " + newState);
        }
    }
}
