using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct SimpleRect
{
    public float xMin;
    public float yMin;
    public float xMax;
    public float yMax;

    public SimpleRect(SimpleRect source)
    {
        xMin = source.xMin;
        yMin = source.yMin;
        xMax = source.xMax;
        yMax = source.yMax;
    }

    public SimpleRect(float xMin, float yMin, float xMax, float yMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
    }

    public bool Contains(Vector2 point)
    {
        return point.x >= xMin &&
               point.y >= yMin &&
               point.x <= xMax &&
               point.y <= yMax;
    }

    public bool Contains(SimpleRect other)
    {
        return other.xMin >= xMin &&
               other.yMin >= yMin &&
               other.xMax <= xMax &&
               other.yMax <= yMax;
    }

    public bool Contains(SimpleRect other, float areaInside)
    {
        // Calculate overlap bounds
        float overlapXMin = Mathf.Max(xMin, other.xMin);
        float overlapYMin = Mathf.Max(yMin, other.yMin);
        float overlapXMax = Mathf.Min(xMax, other.xMax);
        float overlapYMax = Mathf.Min(yMax, other.yMax);

        // If no overlap, return false
        if (overlapXMin >= overlapXMax || overlapYMin >= overlapYMax)
        {
            return false;
        }

        float otherArea = (other.xMax - other.xMin) * (other.yMax - other.yMin);
        float overlapArea = (overlapXMax - overlapXMin) * (overlapYMax - overlapYMin);

        return (overlapArea / otherArea) >= areaInside;
    }

    public override string ToString()
    {
        return $"(xMin: {xMin}, yMin: {yMin}, xMax: {xMax}, yMax: {yMax})";
    }
}

[System.Serializable]
public struct AngledRect
{
    public SimpleRect Rect;
    public float Angle;

    public AngledRect(float xMin, float yMin, float xMax, float yMax, float angle)
    {
        Rect = new SimpleRect(xMin, yMin, xMax, yMax);
        Angle = angle;
    }

    public AngledRect(SimpleRect rect, float angle)
    {
        Rect = rect;
        Angle = angle;
    }

    public bool Contains(Vector2 point)
    {
        Vector2 center = new Vector2((Rect.xMin + Rect.xMax) / 2, (Rect.yMin + Rect.yMax) / 2);
        Vector2 rotatedPoint = RotatePoint(point, center, -Angle);
        return Rect.Contains(rotatedPoint);
    }

    public bool Contains(AngledRect other)
    {
        Vector2 center = new Vector2((Rect.xMin + Rect.xMax) / 2, (Rect.yMin + Rect.yMax) / 2);
        Vector2 rotatedPoint = RotatePoint(new Vector2(other.Rect.xMin, other.Rect.yMin), center, -Angle);
        SimpleRect rotatedRect = new SimpleRect(rotatedPoint.x, rotatedPoint.y, rotatedPoint.x + (other.Rect.xMax - other.Rect.xMin), rotatedPoint.y + (other.Rect.yMax - other.Rect.yMin));
        return Rect.Contains(rotatedRect);
    }

    public bool Contains(AngledRect other, float areaInside)
    {
        Vector2 center = new Vector2((Rect.xMin + Rect.xMax) / 2, (Rect.yMin + Rect.yMax) / 2);
        Vector2 rotatedPoint = RotatePoint(new Vector2(other.Rect.xMin, other.Rect.yMin), center, -Angle);
        SimpleRect rotatedRect = new SimpleRect(rotatedPoint.x, rotatedPoint.y, rotatedPoint.x + (other.Rect.xMax - other.Rect.xMin), rotatedPoint.y + (other.Rect.yMax - other.Rect.yMin));
        return Rect.Contains(rotatedRect, areaInside);
    }

    public static Vector2 RotatePoint(Vector2 point, Vector2 center, float angle)
    {
        float s = Mathf.Sin(angle * Mathf.Deg2Rad);
        float c = Mathf.Cos(angle * Mathf.Deg2Rad);

        point.x -= center.x;
        point.y -= center.y;

        float xnew = point.x * c - point.y * s;
        float ynew = point.x * s + point.y * c;

        point.x = xnew + center.x;
        point.y = ynew + center.y;

        return point;
    }

    public override string ToString()
    {
        return $"(xMin: {Rect.xMin}, yMin: {Rect.yMin}, xMax: {Rect.xMax}, yMax: {Rect.yMax}, Angle: {Angle})";
    }
}

[System.Serializable]
public struct BoundingBoxCorners
{
    public float x1, y1, x2, y2, x3, y3, x4, y4;

    public BoundingBoxCorners(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {
        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
        this.x3 = x3;
        this.y3 = y3;
        this.x4 = x4;
        this.y4 = y4;
    }

    public static BoundingBoxCorners OBBToCorners(float xMin, float yMin, float xMax, float yMax, float angle)
    {
        float x_c = (xMin + xMax) / 2f;
        float y_c = (yMin + yMax) / 2f;
        float w = xMax - xMin;
        float h = yMax - yMin;

        // Define unrotated corner offsets (relative to center)
        Vector2[] corners = new Vector2[4]
        {
            new Vector2(-w / 2f,  h / 2f), // Top-left
            new Vector2( w / 2f,  h / 2f), // Top-right
            new Vector2( w / 2f, -h / 2f), // Bottom-right
            new Vector2(-w / 2f, -h / 2f)  // Bottom-left
        };

        float theta = angle * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(theta);
        float sinTheta = Mathf.Sin(theta);

        // Rotate and translate corners
        for (int i = 0; i < corners.Length; i++)
        {
            float x = corners[i].x;
            float y = corners[i].y;

            float xRotated = cosTheta * x - sinTheta * y;
            float yRotated = sinTheta * x + cosTheta * y;

            corners[i] = new Vector2(x_c + xRotated, y_c + yRotated);
        }

        return new BoundingBoxCorners(
            corners[0].x, corners[0].y,
            corners[1].x, corners[1].y,
            corners[2].x, corners[2].y,
            corners[3].x, corners[3].y
        );
    }
}

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

    public void SetRectTransform(AngledRect angledRect)
    {
        float width = angledRect.Rect.xMax - angledRect.Rect.xMin;
        float height = angledRect.Rect.yMax - angledRect.Rect.yMin;

        RectTransform.anchoredPosition = new Vector2(angledRect.Rect.xMin, angledRect.Rect.yMin);
        RectTransform.sizeDelta = new Vector2(width, height);
        RectTransform.localEulerAngles = new Vector3(0, 0, angledRect.Angle);
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
        DebuggerFactory.DestroyRectDebugger(this);
    }
}
