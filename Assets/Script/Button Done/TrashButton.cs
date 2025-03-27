using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrashButton : MonoBehaviour
{
    public ColoringManager coloringManager;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
    }

    public void OnTrashButtonClick()
    {
        // Hiện popup xác nhận
        ConfirmPopup.Show("Bạn có chắc chắn muốn xóa tất cả không?", ConfirmDelete);
    }

    private void ConfirmDelete(bool confirm)
    {
        if (confirm)
        {
            coloringManager.ClearCanvas();
        }
        ConfirmPopup.Instance.popup.SetActive(false);   
    }
}