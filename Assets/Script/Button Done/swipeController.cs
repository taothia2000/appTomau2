using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swipeController : MonoBehaviour
{
    [SerializeField] int maxPage;
    int curentPage;
    Vector3 targetPos;
    [SerializeField] Vector3 pageStep;
    [SerializeField] RectTransform levelPagesRect;
    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;

    private void Awake()
    {
        curentPage = 1;
        targetPos = levelPagesRect.localPosition;
    }
    public void Next()
    {
        if(curentPage < maxPage)
        {
            curentPage++;
            targetPos += pageStep;
            MovePage();
        }

    }

    public void Previous()
    {
        if(curentPage > 1 )
        {
            curentPage--;
            targetPos -=pageStep;
            MovePage();
        }
        
    }

    void MovePage()
    {
        levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
    }
}
