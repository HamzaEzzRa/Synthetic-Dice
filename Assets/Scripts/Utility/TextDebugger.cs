using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class TextDebugger : MonoBehaviour
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

    public TextMeshProUGUI Text
    {
        get
        {
            if (text == null)
            {
                text = GetComponent<TextMeshProUGUI>();
                if (text == null)
                {
                    text = gameObject.AddComponent<TextMeshProUGUI>();
                }
            }
            return text;
        }
    }

    public ContentSizeFitter ContentSizeFitter
    {
        get
        {
            if (contentSizeFitter == null)
            {
                contentSizeFitter = GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
                }
            }
            return contentSizeFitter;
        }
    }

    private ContentSizeFitter contentSizeFitter;
    private RectTransform rectTransform;
    private TextMeshProUGUI text;

    public void SetRectTransform(SimpleRect rect)
    {
        float width = rect.xMax - rect.xMin;
        float height = rect.yMax - rect.yMin;

        RectTransform.anchoredPosition = new Vector2(rect.xMin, rect.yMin);
        RectTransform.sizeDelta = new Vector2(width, height);
    }

    public void SetRectTransform(AngledRect angledRect)
    {
        float width = angledRect.Rect.xMax - angledRect.Rect.xMin;
        float height = angledRect.Rect.yMax - angledRect.Rect.yMin;

        RectTransform.anchoredPosition = new Vector2(angledRect.Rect.xMin, angledRect.Rect.yMin);
        RectTransform.sizeDelta = new Vector2(width, height);
        RectTransform.localEulerAngles = new Vector3(0, 0, angledRect.Angle);
    }

    public void SetRectTransform(Rect rect)
    {
        RectTransform.anchoredPosition = new Vector2(rect.x, rect.y);
        RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
    }

    public void SetText(string text, int fontSize=24, int minFontSize=14, int maxFontSize=42, FontStyles fontStyle=FontStyles.Normal) {
        Text.SetText(text);
        Text.fontSize = Mathf.Clamp(fontSize, minFontSize, maxFontSize);
        Text.fontStyle = fontStyle;
    }

    public void SetTextColor(Color color)
    {
        Text.color = color;
    }

    public void OnValidate()
    {
        ContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void OnDisable()
    {
        DebuggerFactory.DestroyTextDebugger(this);
    }
}
