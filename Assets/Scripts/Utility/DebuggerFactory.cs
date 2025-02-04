using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DebuggerFactory
{
    public static Queue<RectDebugger> rectDebuggersPool = new Queue<RectDebugger>();

    private static int maxRectDebuggers = 20;

    public static RectDebugger GetOrCreateRectDebugger(SimpleRect simpleRect, string name)
    {
        Canvas debugCanvas = GetOrCreateCanvas();
        RectDebugger rectDebugger = null;

        if (rectDebuggersPool.Count > 0)
        {
            rectDebugger = rectDebuggersPool.Dequeue();

            if (rectDebugger != null)
            {
                rectDebugger.gameObject.SetActive(true);
                rectDebugger.gameObject.name = name;
            }
        }

        if (rectDebugger == null)
        {
            GameObject rectDebuggerObject = new GameObject(name);
            rectDebuggerObject.transform.SetParent(debugCanvas.transform);

            rectDebugger = rectDebuggerObject.AddComponent<RectDebugger>();
            rectDebugger.OnValidate();
        }

        rectDebugger.RectTransform.anchorMin = Vector2.zero;
        rectDebugger.RectTransform.anchorMax = Vector2.zero;
        rectDebugger.RectTransform.pivot = Vector2.zero;

        rectDebugger.SetRectTransform(simpleRect);

        return rectDebugger;
    }

    public static void DisableOrDestroyRectDebugger(RectDebugger rectDebugger)
    {
        //Debug.Log(rectDebuggersPool.Count);
        if (rectDebuggersPool.Count < maxRectDebuggers && rectDebugger.gameObject.activeInHierarchy)
        {
            rectDebugger.gameObject.SetActive(false);
            rectDebuggersPool.Enqueue(rectDebugger);
        }
        else
        {
        #if UNITY_EDITOR
            GameObject.DestroyImmediate(rectDebugger.gameObject);
        #else
            GameObject.Destroy(rectDebugger.gameObject);
        #endif
        }
    }

    private static Canvas GetOrCreateCanvas()
    {
        Canvas debugCanvas = GameObject.Find("Debug Canvas").GetComponent<Canvas>();
        if (debugCanvas == null)
        {
            debugCanvas = new GameObject("Debug Canvas").AddComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = debugCanvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = 1f;
            canvasScaler.referencePixelsPerUnit = 1f;
        }

        return debugCanvas;
    }
}
