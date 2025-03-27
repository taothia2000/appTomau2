using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LoadScene1 : MonoBehaviour, IPointerClickHandler
{
     public string Main; // Tên scene cần load

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(Main);
    }

}
