using UnityEngine;
using UnityEngine.UI;

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

    public Text Text
    {
        get
        {
            if (text == null)
            {
                text = GetComponent<Text>();
                if (text == null)
                {
                    text = gameObject.AddComponent<Text>();
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
    private Text text;

    public void SetRectTransform(SimpleRect rect)
    {
        float width = rect.xMax - rect.xMin;
        float height = rect.yMax - rect.yMin;

        RectTransform.anchoredPosition = new Vector2(rect.xMin, rect.yMin);
        RectTransform.sizeDelta = new Vector2(width, height);
    }

    public void SetRectTransform(Rect rect)
    {
        RectTransform.anchoredPosition = new Vector2(rect.x, rect.y);
        RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
    }

    public void SetText(string text) {
        Text.text = text;
    }

    public void OnValidate()
    {
        Text.color = Color.red;
        Text.fontSize = 24;

        ContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void OnDisable()
    {
        DebuggerFactory.DisableOrDestroyTextDebugger(this);
    }
}
