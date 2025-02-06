using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebuggerFactory
{
    public static Queue<DiceDebugger> diceDebuggersPool = new Queue<DiceDebugger>();
    public static Queue<TextDebugger> textDebuggersPool = new Queue<TextDebugger>();
    public static Queue<RectDebugger> rectDebuggersPool = new Queue<RectDebugger>();

    private static int diceDebuggersPoolSize = 3;
    private static int textDebuggersPoolSize = 3;
    private static int rectDebuggersPoolSize = 3;

    public static TextDebugger GetOrCreateTextDebugger(SimpleRect simpleRect, string textValue, string name)
    {
        Canvas debugCanvas = GetOrCreateCanvas();
        TextDebugger textDebugger = null;

        if (textDebuggersPool.Count > 0)
        {
            textDebugger = textDebuggersPool.Dequeue();
            if (textDebugger != null)
            {
                textDebugger.gameObject.SetActive(true);
                textDebugger.gameObject.name = name;
            }
        }

        if (textDebugger == null)
        {
            GameObject textDebuggerObject = new GameObject(name);
            textDebuggerObject.transform.SetParent(debugCanvas.transform);
            textDebugger = textDebuggerObject.AddComponent<TextDebugger>();
            textDebugger.SetText(textValue);
            textDebugger.OnValidate();
        }

        textDebugger.RectTransform.anchorMin = Vector2.zero;
        textDebugger.RectTransform.anchorMax = Vector2.zero;
        textDebugger.RectTransform.pivot = Vector2.zero;

        textDebugger.SetRectTransform(simpleRect);
        return textDebugger;
    }

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

    public static DiceDebugger GetOrCreateDiceDebugger(SimpleRect simpleRect, string textValue, string name)
    {
        Canvas debugCanvas = GetOrCreateCanvas();
        DiceDebugger diceDebugger = null;
        if (diceDebuggersPool.Count > 0)
        {
            diceDebugger = diceDebuggersPool.Dequeue();
            if (diceDebugger != null)
            {
                diceDebugger.gameObject.SetActive(true);
                diceDebugger.gameObject.name = name;
            }
        }
        if (diceDebugger == null)
        {
            GameObject diceDebuggerObject = new GameObject(name);
            diceDebuggerObject.transform.SetParent(debugCanvas.transform);
            diceDebugger = diceDebuggerObject.AddComponent<DiceDebugger>();
            diceDebugger.OnValidate();
        }

        diceDebugger.RectTransform.anchorMin = Vector2.zero;
        diceDebugger.RectTransform.anchorMax = Vector2.zero;
        diceDebugger.RectTransform.pivot = Vector2.zero;

        diceDebugger.UpdateDebugger(simpleRect, textValue);
        return diceDebugger;
    }

    public static void DisableOrDestroyTextDebugger(TextDebugger textDebugger)
    {
        if (textDebuggersPool.Count < textDebuggersPoolSize && textDebugger.gameObject.activeInHierarchy)
        {
            textDebugger.gameObject.SetActive(false);
            textDebuggersPool.Enqueue(textDebugger);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                GameObject.DestroyImmediate(textDebugger.gameObject);
            };
#else
            GameObject.Destroy(textDebugger.gameObject);
#endif
        }
    }

    public static void DisableOrDestroyRectDebugger(RectDebugger rectDebugger)
    {
        if (rectDebuggersPool.Count < rectDebuggersPoolSize && rectDebugger.gameObject.activeInHierarchy)
        {
            rectDebugger.gameObject.SetActive(false);
            rectDebuggersPool.Enqueue(rectDebugger);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                GameObject.DestroyImmediate(rectDebugger.gameObject);
            };
#else
            GameObject.Destroy(rectDebugger.gameObject);
#endif
        }
    }

    public static void DisableOrDestroyDiceDebugger(DiceDebugger diceDebugger)
    {
        if (diceDebuggersPool.Count < diceDebuggersPoolSize && diceDebugger.gameObject.activeInHierarchy)
        {
            diceDebugger.gameObject.SetActive(false);
            diceDebuggersPool.Enqueue(diceDebugger);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                GameObject.DestroyImmediate(diceDebugger.gameObject);
            };
#else
            GameObject.Destroy(diceDebugger.gameObject);
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
