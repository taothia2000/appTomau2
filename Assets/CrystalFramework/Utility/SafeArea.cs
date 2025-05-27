using UnityEngine;
using System.Collections.Generic; // Để sử dụng Dictionary

namespace Crystal
{
    public class SafeArea : MonoBehaviour
    {
        #region Simulations
        public enum SimDevice
        {
            None,
            iPhoneX,
            iPhoneXsMax,
            Pixel3XL_LSL,
            Pixel3XL_LSR
        }

        public static SimDevice Sim = SimDevice.None;

        Rect[] NSA_iPhoneX = new Rect[]
        {
            new Rect (0f, 102f / 2436f, 1f, 2202f / 2436f),  // Portrait
            new Rect (132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f)  // Landscape
        };

        Rect[] NSA_iPhoneXsMax = new Rect[]
        {
            new Rect (0f, 102f / 2688f, 1f, 2454f / 2688f),  // Portrait
            new Rect (132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f)  // Landscape
        };

        Rect[] NSA_Pixel3XL_LSL = new Rect[]
        {
            new Rect (0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new Rect (0f, 0f, 2789f / 2960f, 1f)  // Landscape
        };

        Rect[] NSA_Pixel3XL_LSR = new Rect[]
        {
            new Rect (0f, 0f, 1f, 2789f / 2960f),  // Portrait
            new Rect (171f / 2960f, 0f, 2789f / 2960f, 1f)  // Landscape
        };
        #endregion

        RectTransform Panel;
        Rect LastSafeArea = new Rect(0, 0, 0, 0);
        Vector2Int LastScreenSize = new Vector2Int(0, 0);
        ScreenOrientation LastOrientation = ScreenOrientation.AutoRotation;
        [SerializeField] bool ConformX = true;
        [SerializeField] bool ConformY = true;
        [SerializeField] bool Logging = false;
        [SerializeField] float ScaleMultiplier = 1f; // Hệ số điều chỉnh để nhân với localScale gốc

        // Lưu trữ localScale ban đầu của các phần tử con
        private Dictionary<RectTransform, Vector3> originalScales = new Dictionary<RectTransform, Vector3>();

        void Awake()
        {
            Panel = GetComponent<RectTransform>();

            if (Panel == null)
            {
                Debug.LogError("Cannot apply safe area - no RectTransform found on " + name);
                Destroy(gameObject);
            }

            // Lưu trữ localScale ban đầu của tất cả phần tử con
            foreach (RectTransform child in Panel.GetComponentsInChildren<RectTransform>())
            {
                if (child == Panel) continue;
                originalScales[child] = child.localScale;
            }

            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            Rect safeArea = GetSafeArea();

            if (safeArea != LastSafeArea
                || Screen.width != LastScreenSize.x
                || Screen.height != LastScreenSize.y
                || Screen.orientation != LastOrientation)
            {
                LastScreenSize.x = Screen.width;
                LastScreenSize.y = Screen.height;
                LastOrientation = Screen.orientation;

                ApplySafeArea(safeArea);
            }
        }

        Rect GetSafeArea()
        {
            Rect safeArea = Screen.safeArea;

            if (Application.isEditor && Sim != SimDevice.None)
            {
                Rect nsa = new Rect(0, 0, Screen.width, Screen.height);

                switch (Sim)
                {
                    case SimDevice.iPhoneX:
                        nsa = Screen.height > Screen.width ? NSA_iPhoneX[0] : NSA_iPhoneX[1];
                        break;
                    case SimDevice.iPhoneXsMax:
                        nsa = Screen.height > Screen.width ? NSA_iPhoneXsMax[0] : NSA_iPhoneXsMax[1];
                        break;
                    case SimDevice.Pixel3XL_LSL:
                        nsa = Screen.height > Screen.width ? NSA_Pixel3XL_LSL[0] : NSA_Pixel3XL_LSL[1];
                        break;
                    case SimDevice.Pixel3XL_LSR:
                        nsa = Screen.height > Screen.width ? NSA_Pixel3XL_LSR[0] : NSA_Pixel3XL_LSR[1];
                        break;
                }

                safeArea = new Rect(Screen.width * nsa.x, Screen.height * nsa.y, Screen.width * nsa.width, Screen.height * nsa.height);
            }

            return safeArea;
        }

        void ApplySafeArea(Rect r)
        {
            LastSafeArea = r;

            if (!ConformX)
            {
                r.x = 0;
                r.width = Screen.width;
            }

            if (!ConformY)
            {
                r.y = 0;
                r.height = Screen.height;
            }

            if (Screen.width > 0 && Screen.height > 0)
            {
                Vector2 anchorMin = r.position;
                Vector2 anchorMax = r.position + r.size;
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;

                if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
                {
                    Panel.anchorMin = anchorMin;
                    Panel.anchorMax = anchorMax;

                    // Điều chỉnh tỷ lệ cho các phần tử con
                    foreach (RectTransform child in Panel.GetComponentsInChildren<RectTransform>())
                    {
                        if (child == Panel) continue;

                        // Lấy localScale ban đầu từ dictionary
                        Vector3 originalScale = originalScales.ContainsKey(child) ? originalScales[child] : child.localScale;

                        // Tính hệ số dựa trên vùng an toàn
                        float scaleX = r.width / Screen.width;
                        float scaleY = r.height / Screen.height;
                        float safeAreaScale = Mathf.Min(scaleX, scaleY);

                        // Nhân localScale ban đầu với hệ số điều chỉnh
                        float adjustedScale = safeAreaScale * ScaleMultiplier;
                        child.localScale = new Vector3(
                            originalScale.x * adjustedScale,
                            originalScale.y * adjustedScale,
                            originalScale.z
                        );
                    }
                }
            }

            if (Logging)
            {
                Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}, scale multiplier={7}",
                    name, r.x, r.y, r.width, r.height, Screen.width, Screen.height, ScaleMultiplier);
            }
        }
    }
}