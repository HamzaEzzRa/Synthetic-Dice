using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebuggerFactory
{
    public static TextDebugger CreateTextDebugger(AngledRect rect, string textValue, string name)
    {
        Canvas debugCanvas = GetOrCreateCanvas();

        GameObject textDebuggerObject = new GameObject(name);
        textDebuggerObject.transform.SetParent(debugCanvas.transform);
        
        TextDebugger textDebugger = textDebuggerObject.AddComponent<TextDebugger>();
        textDebugger.SetText(textValue);
        textDebugger.OnValidate();

        textDebugger.RectTransform.anchorMin = Vector2.zero;
        textDebugger.RectTransform.anchorMax = Vector2.zero;
        textDebugger.RectTransform.pivot = Vector2.zero;

        textDebugger.SetRectTransform(rect);
        return textDebugger;
    }

    public static RectDebugger CreateRectDebugger(AngledRect rect, string name)
    {
        Canvas debugCanvas = GetOrCreateCanvas();

        GameObject rectDebuggerObject = new GameObject(name);
        rectDebuggerObject.transform.SetParent(debugCanvas.transform);

        RectDebugger rectDebugger = rectDebuggerObject.AddComponent<RectDebugger>();
        rectDebugger.OnValidate();

        rectDebugger.RectTransform.anchorMin = Vector2.zero;
        rectDebugger.RectTransform.anchorMax = Vector2.zero;
        rectDebugger.RectTransform.pivot = Vector2.zero;

        rectDebugger.SetRectTransform(rect);
        return rectDebugger;
    }

    public static DiceDebugger CreateDiceDebugger(SimpleRect rect, string textValue, string name)
    {
        return CreateDiceDebugger(new AngledRect(rect, 0f), textValue, name);
    }

    public static DiceDebugger CreateDiceDebugger(AngledRect rect, string textValue, string name)
    {
        Canvas debugCanvas = GetOrCreateCanvas();

        GameObject diceDebuggerObject = new GameObject(name);
        diceDebuggerObject.transform.SetParent(debugCanvas.transform);

        DiceDebugger diceDebugger = diceDebuggerObject.AddComponent<DiceDebugger>();
        diceDebugger.OnValidate();

        diceDebugger.RectTransform.anchorMin = Vector2.one * 0.5f;
        diceDebugger.RectTransform.anchorMax = Vector2.one * 0.5f;
        diceDebugger.RectTransform.pivot = Vector2.one * 0.5f;

        diceDebugger.UpdateDebugger(rect, textValue);
        return diceDebugger;
    }

    public static void DestroyTextDebugger(TextDebugger textDebugger)
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

    public static void DestroyRectDebugger(RectDebugger rectDebugger)
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

    public static void DestroyDiceDebugger(DiceDebugger diceDebugger)
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
