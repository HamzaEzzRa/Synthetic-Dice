using System.Collections.Generic;
using UnityEngine;

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
public class DiceMeshData
{
    public Mesh Mesh;
    public Vector3[] ValueToRotation;
}

[ExecuteAlways]
public class DiceRandomizer : Randomizer
{
    public static List<DiceRandomizer> RandomizedDice = new List<DiceRandomizer>();

    [SerializeField] private Camera renderCamera;

    [SerializeField] private DiceMeshData[] meshData;

    [SerializeField] private Color[] diceColors;
    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange diceMetallic;
    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange diceSmoothness;

    [SerializeField] private Color[] dotColors;
    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange dotMetallic;
    [SerializeField, FloatRangeSlider(0f, 1f)] private FloatRange dotSmoothness;

    [SerializeField] private BoxCollider ground;
    [SerializeField] private bool randomizePosition;
    [SerializeField] private bool randomizeRotationY;
    [SerializeField, FloatRangeSlider(0.1f, 2f)] private FloatRange diceScaleRange;
    [SerializeField, Range(0f, 1f)] private float diceProbability = 0.5f;

    [Header("Bounding Box")]
    [SerializeField] private CameraRandomizer cameraRandomizer;
    [SerializeField] private bool debug;

    public bool Enabled
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        private set
        {
            if (!value)
            {
                CurrentValue = 0;

                if (boundingRectDebugger != null)
                {
                    DebuggerFactory.DisableOrDestroyRectDebugger(boundingRectDebugger);
                }
                BoundingRect = new SimpleRect(0f, 0f, 0f, 0f);
            }

            gameObject.SetActive(value);
        }
    }

    public MeshCollider DiceCollider
    {
        get
        {
            if (diceCollider == null)
            {
                diceCollider = GetComponent<MeshCollider>();
            }

            return diceCollider;
        }
    }

    public Renderer DiceRenderer
    {
        get
        {
            if (diceRenderer == null)
            {
                diceRenderer = GetComponent<Renderer>();
            }

            return diceRenderer;
        }
    }

    public MeshFilter DiceMeshFilter
    {
        get
        {
            if (diceMeshFilter == null)
            {
                diceMeshFilter = GetComponent<MeshFilter>();
            }

            return diceMeshFilter;
        }
    }

    public int CurrentValue { get; private set; }
    public BoxCollider Ground => ground;

    public SimpleRect BoundingRect { get; private set; }

    private MeshCollider diceCollider;
    private Renderer diceRenderer;
    private MeshFilter diceMeshFilter;

    [SerializeField] private DiceMeshData currentMeshData;

    [SerializeField, HideInInspector] private RectDebugger boundingRectDebugger;

    private void Update()
    {
        if (Enabled)
        {
            GetBoundingBox();
        }
    }

    public override void Randomize()
    {
        if (Random.Range(0f, 1f) > diceProbability)
        {
            Enabled = false;
        }
        else
        {
            Enabled = true;

            bool collidesWithOtherDice;
            do
            {
                if (randomizePosition)
                {
                    RandomizePosition();
                }

                RandomizeMesh();
                RandomizeRotation(currentMeshData.ValueToRotation);
                RandomizeScale();
                RandomizeColor();

                Physics.SyncTransforms();
                GetBoundingBox();

                //Debug.Log(name + " randomized. Checking for collisions...");
                collidesWithOtherDice = false;
                foreach (DiceRandomizer die in RandomizedDice)
                {
                    if (die == this) continue;
                    //Debug.Log("Checking with " + die.name);

                    Bounds dieBounds = die.DiceCollider.bounds;
                    if (dieBounds.Intersects(DiceCollider.bounds))
                    {
                        collidesWithOtherDice = true;
                        //Debug.Log("Collides with other dice, retrying...");
                        break;
                    }
                }
            } while (collidesWithOtherDice);

            RandomizedDice.Add(this);
        }
    }

    private void RandomizeRotation(Vector3[] valueToRotation)
    {
        CurrentValue = Random.Range(0, valueToRotation.Length) + 1;
        Vector3 eulerAngles = valueToRotation[CurrentValue - 1];
        //Debug.Log("Current Value: " + CurrentValue);

        if (randomizeRotationY)
        {
            eulerAngles.y = Random.Range(0f, 359f);
        }

        transform.localRotation = Quaternion.Euler(eulerAngles);
    }

    private void RandomizePosition()
    {
        Vector3Range boundRange = new Vector3Range(ground.bounds.min, ground.bounds.max);
        Vector3 randomPosition = boundRange.RandomInRange;
        randomPosition.y = transform.localPosition.y;

        Vector3 extents = DiceCollider.bounds.extents;
        randomPosition.x = randomPosition.x + -Mathf.Sign(randomPosition.x) * extents.x;
        randomPosition.z = randomPosition.z + -Mathf.Sign(randomPosition.z) * extents.z;

        transform.localPosition = randomPosition;
    }

    private void RandomizeScale()
    {
        Vector3 randomScale = Vector3.one * diceScaleRange.RandomInRange;
        transform.localScale = randomScale;

        // Keep dice grounded
        transform.localPosition = new Vector3(transform.localPosition.x, (transform.localScale.y - 1f) / 2f, transform.localPosition.z);
    }

    private void RandomizeColor()
    {
        Color dotColor = dotColors[Random.Range(0, dotColors.Length)];
        Color diceColor = diceColors[Random.Range(0, diceColors.Length)];
        while (diceColor == dotColor)
        {
            diceColor = diceColors[Random.Range(0, diceColors.Length)];
        }

        DiceRenderer.sharedMaterials[0].color = diceColor;
        diceRenderer.sharedMaterials[0].SetFloat("_Metallic", diceMetallic.RandomInRange);
        diceRenderer.sharedMaterials[0].SetFloat("_Glossiness", diceSmoothness.RandomInRange);

        DiceRenderer.sharedMaterials[1].color = dotColor;
        diceRenderer.sharedMaterials[1].SetFloat("_Metallic", dotMetallic.RandomInRange);
        diceRenderer.sharedMaterials[1].SetFloat("_Glossiness", dotSmoothness.RandomInRange);
    }

    private void RandomizeMesh()
    {
        currentMeshData = meshData[Random.Range(0, meshData.Length)];

        Mesh diceMesh = currentMeshData.Mesh;
        DiceMeshFilter.sharedMesh = diceMesh;
        DiceCollider.sharedMesh = diceMesh;
    }

    private void GetBoundingBox()
    {
        Bounds diceBounds = DiceRenderer.bounds;

        Vector3[] diceCorners = new Vector3[8];
        diceCorners[0] = diceBounds.min;
        diceCorners[1] = new Vector3(diceBounds.max.x, diceBounds.min.y, diceBounds.min.z);
        diceCorners[2] = new Vector3(diceBounds.min.x, diceBounds.max.y, diceBounds.min.z);
        diceCorners[3] = new Vector3(diceBounds.max.x, diceBounds.max.y, diceBounds.min.z);
        diceCorners[4] = new Vector3(diceBounds.min.x, diceBounds.min.y, diceBounds.max.z);
        diceCorners[5] = new Vector3(diceBounds.max.x, diceBounds.min.y, diceBounds.max.z);
        diceCorners[6] = new Vector3(diceBounds.min.x, diceBounds.max.y, diceBounds.max.z);
        diceCorners[7] = diceBounds.max;

        //Need to figure this out if we want y-axis rotation of the dice (for now rotate the camera)
        //for (int i = 0; i < diceCorners.Length; i++)
        //{
        //    diceCorners[i] = transform.TransformPoint(diceCorners[i]);
        //}

        Vector3 screenMin = new Vector3(float.MaxValue, float.MaxValue);
        Vector3 screenMax = new Vector3(float.MinValue, float.MinValue);

        for (int i = 0; i < diceCorners.Length; i++)
        {
            Vector3 screenPoint = renderCamera.WorldToScreenPoint(diceCorners[i]);
            if (screenPoint.z < 0)
            {
                continue; // Ignore corners behind the camera
            }

            screenMin = new Vector3(
                Mathf.Min(screenMin.x, screenPoint.x),
                Mathf.Min(screenMin.y, screenPoint.y)
            );
            screenMax = new Vector3(
                Mathf.Max(screenMax.x, screenPoint.x),
                Mathf.Max(screenMax.y, screenPoint.y)
            );
        }

        //float width = screenMax.x - screenMin.x;
        //if (width > maxBBSize.x)
        //{
        //    float excessWidth = width - maxBBSize.x;
        //    screenMin.x += excessWidth / 2f;
        //    screenMax.x -= excessWidth / 2f;
        //    width = maxBBSize.x;
        //}

        //float height = screenMax.y - screenMin.y;
        //if (height > maxBBSize.y)
        //{
        //    float excessHeight = height - maxBBSize.y;
        //    screenMin.y += excessHeight / 2f;
        //    screenMax.y -= excessHeight / 2f;
        //    height = maxBBSize.y;
        //}

        BoundingRect = new SimpleRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
        if (debug)
        {
            if (boundingRectDebugger == null || !boundingRectDebugger.gameObject.activeInHierarchy)
            {
                boundingRectDebugger = DebuggerFactory.GetOrCreateRectDebugger(BoundingRect, "BBox Debugger " + name);
            }
            else
            {
                boundingRectDebugger.SetRectTransform(BoundingRect);
            }
        }
    }
}
