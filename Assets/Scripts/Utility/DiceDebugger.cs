using TMPro;
using UnityEngine;

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

        RectTransform.position = new Vector2(rect.xMin + width / 2f, rect.yMin + height / 2f);
        RectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.localEulerAngles = Vector3.zero;

        DiceBBoxDebugger.RectTransform.sizeDelta = new Vector2(width, height);

        float textY = height;
        DiceValueDebugger.RectTransform.anchoredPosition = new Vector2(0f, textY);

        int fontSize = (int)(24 * height / 60f);
        FontStyles fontStyle = fontSize < 24 ? FontStyles.Bold : FontStyles.Normal;
        DiceValueDebugger.SetText(text, fontSize, fontStyle:fontStyle);
        DiceValueDebugger.SetTextColor(Color.red);
    }

    public void UpdateDebugger(AngledRect angledRect, string text)
    {
        float width = angledRect.Rect.xMax - angledRect.Rect.xMin;
        float height = angledRect.Rect.yMax - angledRect.Rect.yMin;

        RectTransform.position = new Vector2(angledRect.Rect.xMin + width / 2f, angledRect.Rect.yMin + height / 2f);
        RectTransform.sizeDelta = new Vector2(width, height);
        RectTransform.localEulerAngles = new Vector3(0f, 0f, angledRect.Angle);

        DiceBBoxDebugger.RectTransform.sizeDelta = new Vector2(width, height);

        float textY = height;
        DiceValueDebugger.RectTransform.anchoredPosition = new Vector2(0f, textY);

        int fontSize = (int)(24 * height / 60f);
        FontStyles fontStyle = fontSize < 24 ? FontStyles.Bold : FontStyles.Normal;
        DiceValueDebugger.SetText(text, fontSize, fontStyle:fontStyle);
        DiceValueDebugger.SetTextColor(Color.red);
    }

    public void UpdateDebugger(Rect rect, string text)
    {
        RectTransform.position = new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f);
        RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
        rectTransform.localEulerAngles = Vector3.zero;

        DiceBBoxDebugger.RectTransform.sizeDelta = new Vector2(rect.width, rect.height);

        float textY = rect.height;
        DiceValueDebugger.RectTransform.anchoredPosition = new Vector2(0f, textY);

        int fontSize = (int)(24 * rect.height / 60f);
        FontStyles fontStyle = fontSize < 24 ? FontStyles.Bold : FontStyles.Normal;
        DiceValueDebugger.SetText(text, fontSize, fontStyle:fontStyle);
        DiceValueDebugger.SetTextColor(Color.red);
    }

    public void OnValidate()
    {
        DiceValueDebugger.RectTransform.SetParent(RectTransform);
        DiceBBoxDebugger.RectTransform.SetParent(RectTransform);

        DiceValueDebugger.RectTransform.anchorMin = Vector2.zero;
        DiceValueDebugger.RectTransform.anchorMax = Vector2.zero;
        DiceValueDebugger.RectTransform.pivot = Vector2.zero;
        DiceValueDebugger.RectTransform.anchoredPosition = Vector2.zero;

        DiceBBoxDebugger.RectTransform.anchorMin = Vector2.one * 0.5f;
        DiceBBoxDebugger.RectTransform.anchorMax = Vector2.one * 0.5f;
        DiceBBoxDebugger.RectTransform.pivot = Vector2.one * 0.5f;
        DiceBBoxDebugger.RectTransform.anchoredPosition = Vector2.zero;
    }

    private void OnDisable()
    {
        DebuggerFactory.DestroyDiceDebugger(this);
    }
}
