using UnityEngine;

public class DebugConnections : MonoBehaviour
{
    public void Start()
    {
        // Kiểm tra kết nối của tất cả ColorButton trong scene
        ColorButton[] allButtons = FindObjectsOfType<ColorButton>();
        
        Debug.Log($"Found {allButtons.Length} ColorButton objects in scene");
        
        foreach (var button in allButtons)
        {
            Debug.Log($"Button: {button.gameObject.name}");
            
            if (button.colorCustomizer == null)
            {
                Debug.LogError($"Button {button.gameObject.name} does not have a colorCustomizer reference!");
            }
            else
            {
                Debug.Log($"Button {button.gameObject.name} has colorCustomizer: {button.colorCustomizer.gameObject.name}");
                
                if (button.colorCustomizer.colorPalettePanel == null)
                {
                    Debug.LogError($"ColorButtonCustomizer on {button.colorCustomizer.gameObject.name} does not have a colorPalettePanel reference!");
                }
                else
                {
                    Debug.Log($"ColorPalettePanel is set to: {button.colorCustomizer.colorPalettePanel.name}");
                    
                    // Kiểm tra xem panel có active không
                    Debug.Log($"Panel is currently {(button.colorCustomizer.colorPalettePanel.activeSelf ? "ACTIVE" : "INACTIVE")}");
                }
            }
            
            // Kiểm tra EventTrigger
            EventTriggerSetup eventSetup = button.GetComponent<EventTriggerSetup>();
            if (eventSetup == null)
            {
                Debug.LogError($"Button {button.gameObject.name} does not have EventTriggerSetup component!");
            }
        }
    }
}