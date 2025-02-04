using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class RectDebugger : MonoBehaviour
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

    public Image Image
    {
        get
        {
            if (image == null)
            {
                image = GetComponent<Image>();
                if (image == null)
                {
                    image = gameObject.AddComponent<Image>();
                }
            }
            return image;
        }
    }

    private RectTransform rectTransform;
    private Image image;

    public void SetRectTransform(SimpleRect simpleRect)
    {
        float width = simpleRect.xMax - simpleRect.xMin;
        float height = simpleRect.yMax - simpleRect.yMin;

        RectTransform.anchoredPosition = new Vector2(simpleRect.xMin, simpleRect.yMin);
        RectTransform.sizeDelta = new Vector2(width, height);
    }

    public void SetRectTransform(Rect rect)
    {
        RectTransform.anchoredPosition = new Vector2(rect.x, rect.y);
        RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
    }

    public void OnValidate()
    {
        Image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Box.png");
        Image.color = Color.red;
        Image.raycastTarget = false;

        Image.maskable = true;
        Image.type = Image.Type.Sliced;
        Image.fillCenter = true;
        Image.pixelsPerUnitMultiplier = 0.05f;
    }

    private void OnDisable()
    {
        DebuggerFactory.DisableOrDestroyRectDebugger(this);
    }
}
