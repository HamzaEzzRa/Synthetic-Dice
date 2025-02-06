using UnityEngine;
using UnityEngine.UI;

public class DiceDebugger : MonoBehaviour
{
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    rectTransform = gameObject.AddComponent<RectTransform>();
                }
            }
            return rectTransform;
        }
    }

    public TextDebugger DiceValueDebugger
    {
        get
        {
            if (diceValueDebugger == null)
            {
                diceValueDebugger = GetComponentInChildren<TextDebugger>();
                if (diceValueDebugger == null)
                {
                    GameObject gameObject = new GameObject("DiceValueDebugger");
                    diceValueDebugger = gameObject.AddComponent<TextDebugger>();
                }
            }
            return diceValueDebugger;
        }
    }

    public RectDebugger DiceBBoxDebugger
    {
        get
        {
            if (diceBBoxDebugger == null)
            {
                diceBBoxDebugger = GetComponentInChildren<RectDebugger>();
                if (diceBBoxDebugger == null)
                {
                    GameObject gameObject = new GameObject("DiceBBoxDebugger");
                    diceBBoxDebugger = gameObject.AddComponent<RectDebugger>();
                }
            }
            return diceBBoxDebugger;
        }
    }

    private RectTransform rectTransform;
    private TextDebugger diceValueDebugger;
    private RectDebugger diceBBoxDebugger;

    public void UpdateDebugger(SimpleRect rect, string text)
    {
        float width = rect.xMax - rect.xMin;
        float height = rect.yMax - rect.yMin;

        RectTransform.anchoredPosition = new Vector2(rect.xMin, rect.yMin);
        RectTransform.sizeDelta = new Vector2(width, height);

        DiceBBoxDebugger.RectTransform.sizeDelta = new Vector2(width, height);

        float textHeight = height;
        DiceValueDebugger.SetRectTransform(new SimpleRect(0f, textHeight, 0f, textHeight));
        DiceValueDebugger.SetText(text);
    }

    public void UpdateDebugger(Rect rect, string text)
    {
        RectTransform.anchoredPosition = new Vector2(rect.x, rect.y);
        RectTransform.sizeDelta = new Vector2(rect.width, rect.height);

        DiceBBoxDebugger.RectTransform.sizeDelta = new Vector2(rect.width, rect.height);

        float textHeight = rect.height;
        DiceValueDebugger.SetRectTransform(new SimpleRect(0f, textHeight, 0f, textHeight));
        DiceValueDebugger.SetText(text);
    }

    public void OnValidate()
    {
        DiceValueDebugger.RectTransform.SetParent(RectTransform);
        DiceBBoxDebugger.RectTransform.SetParent(RectTransform);

        DiceValueDebugger.RectTransform.anchorMin = Vector2.zero;
        DiceValueDebugger.RectTransform.anchorMax = Vector2.zero;
        DiceValueDebugger.RectTransform.pivot = Vector2.zero;
        DiceValueDebugger.RectTransform.anchoredPosition = Vector2.zero;

        DiceBBoxDebugger.RectTransform.anchorMin = Vector2.zero;
        DiceBBoxDebugger.RectTransform.anchorMax = Vector2.zero;
        DiceBBoxDebugger.RectTransform.pivot = Vector2.zero;
        DiceBBoxDebugger.RectTransform.anchoredPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        DebuggerFactory.DisableOrDestroyDiceDebugger(this);
    }
}
