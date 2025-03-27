using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenShop : MonoBehaviour
{
    public void Linkgame()
    {
        #if PLATFORM_ANDROID
            Application.OpenURL("https://play.google.com/store/apps/details?id=mobi.mgh.mong.daihiep");
        #else
            Application.OpenURL("https://legensign.com/download/173470725916765883b31c0b");
        #endif    

    
    }
}