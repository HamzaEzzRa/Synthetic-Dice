using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class DiceMeshData
{
    public Mesh Mesh;
    public Vector3[] ValueToRotation;
}

[ExecuteAlways]
public class DiceRandomizer : Randomizer
{
    public enum DebugMode
    {
        NONE,
        AXIS_ALIGNED,
        ORIENTED
    }

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
    [SerializeField, Range(0f, 1f)] private float meshContribution = 0.5f;
    [SerializeField] private DebugMode debugMode;

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
                BoundingRect = new SimpleRect(0f, 0f, 0f, 0f);
                AngledBoundingRect = new AngledRect(0f, 0f, 0f, 0f, 0f);
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
    public AngledRect AngledBoundingRect { get; private set; }

    private MeshCollider diceCollider;
    private Renderer diceRenderer;
    private MeshFilter diceMeshFilter;

    [SerializeField] private DiceMeshData currentMeshData;

    [SerializeField] private DiceDebugger diceDebugger;

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
        Mesh mesh = diceMeshFilter.sharedMesh;
        Vector3[] meshPoints;
        if (mesh.isReadable && meshContribution > 0f)
        {
            List<Vector3> meshVertices = mesh.vertices.ToList();
            // Randomly sample vertices up to meshContribution
            int numSamples = (int)(meshContribution * meshVertices.Count);
            meshPoints = new Vector3[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                int rndIndex = Random.Range(0, meshVertices.Count);
                meshPoints[i] = meshVertices[rndIndex];
                meshVertices.RemoveAt(rndIndex);
                meshVertices.TrimExcess();
            }
        }
        else
        {
            if (!mesh.isReadable)
            {
                Debug.LogWarning("Mesh is not readable. Using renderer bounds instead (faster but less accurate)");
            }

            Bounds bounds = DiceRenderer.localBounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            meshPoints = new Vector3[8]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
            };
        }

        Vector3[] worldPoints = new Vector3[meshPoints.Length];
        for (int i = 0; i < meshPoints.Length; i++)
        {
            worldPoints[i] = transform.TransformPoint(meshPoints[i]);
        }

        Vector2[] screenPoints = new Vector2[worldPoints.Length];
        for (int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 screenPoint = renderCamera.WorldToScreenPoint(worldPoints[i]);
            screenPoints[i] = new Vector2(screenPoint.x, screenPoint.y);
        }

        // Calculate the axis-aligned bounding box
        BoundingRect = new SimpleRect(
            screenPoints.Min(p => p.x), screenPoints.Min(p => p.y),
            screenPoints.Max(p => p.x), screenPoints.Max(p => p.y)
        );

        // Calculate the oriented bounding box
        Vector2[] diceHull = ComputeConvexHull(screenPoints); // Compute convex hull in 2D screen space
        AngledBoundingRect = ComputeOrientedBoundingBox(diceHull); // Compute the minimum-area bounding rectangle

        if (debugMode == DebugMode.NONE)
        {
            if (diceDebugger != null)
            {
                DebuggerFactory.DestroyDiceDebugger(diceDebugger);
                diceDebugger = null;
            }
        }
        else
        {
            if (diceDebugger == null || !diceDebugger.gameObject.activeInHierarchy)
            {
                if (debugMode == DebugMode.AXIS_ALIGNED)
                {
                    diceDebugger = DebuggerFactory.CreateDiceDebugger(BoundingRect, CurrentValue.ToString(), $"{name} Debugger");
                }
                else if (debugMode == DebugMode.ORIENTED)
                {
                    diceDebugger = DebuggerFactory.CreateDiceDebugger(AngledBoundingRect, CurrentValue.ToString(), $"{name} Debugger");
                }
            }
            else
            {
                if (debugMode == DebugMode.AXIS_ALIGNED)
                {
                    diceDebugger.UpdateDebugger(BoundingRect, CurrentValue.ToString());
                }
                else if (debugMode == DebugMode.ORIENTED)
                {
                    diceDebugger.UpdateDebugger(AngledBoundingRect, CurrentValue.ToString());
                }
            }
        }
    }

    // Rotating Calipers algorithm
    private AngledRect ComputeOrientedBoundingBox(Vector2[] hullPoints)
    {
        if (hullPoints.Length < 2) return new AngledRect(0, 0, 0, 0, 0);

        int numPoints = hullPoints.Length;
        float minArea = float.MaxValue;
        Vector2 bestCenter = Vector2.zero;
        Vector2 bestSize = Vector2.zero;
        float bestAngle = 0f;

        for (int i = 0; i < numPoints; i++)
        {
            // Get edge vector (hull[i] to hull[i+1])
            Vector2 edge = hullPoints[(i + 1) % numPoints] - hullPoints[i];
            float edgeAngle = Mathf.Atan2(edge.y, edge.x);

            // Create a matrix for contiguous and fast rotation
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -edgeAngle * Mathf.Rad2Deg));

            // Rotate all hull points into a local frame aligned with the edge
            Vector2[] rotatedPoints = new Vector2[numPoints];
            for (int j = 0; j < numPoints; j++)
            {
                Vector3 rotated = rotationMatrix.MultiplyPoint3x4(new Vector3(hullPoints[j].x, hullPoints[j].y, 0));
                rotatedPoints[j] = new Vector2(rotated.x, rotated.y);
            }

            // Compute min/max in rotated space
            float xMin = rotatedPoints.Min(p => p.x);
            float xMax = rotatedPoints.Max(p => p.x);
            float yMin = rotatedPoints.Min(p => p.y);
            float yMax = rotatedPoints.Max(p => p.y);

            float width = xMax - xMin;
            float height = yMax - yMin;
            float area = width * height;

            if (area < minArea)
            {
                minArea = area;
                bestSize = new Vector2(width, height);
                bestAngle = edgeAngle;

                // Compute the actual center in world coordinates
                Vector2 centerRotated = new Vector2((xMin + xMax) / 2, (yMin + yMax) / 2);
                Vector3 centerWorld = rotationMatrix.inverse.MultiplyPoint3x4(new Vector3(centerRotated.x, centerRotated.y, 0));

                bestCenter = new Vector2(centerWorld.x, centerWorld.y);
            }
        }

        return new AngledRect(
            bestCenter.x - bestSize.x / 2, bestCenter.y - bestSize.y / 2,
            bestCenter.x + bestSize.x / 2, bestCenter.y + bestSize.y / 2,
            bestAngle * Mathf.Rad2Deg
        );
    }

    private Vector2[] ComputeConvexHull(Vector2[] points)
    {
        if (points.Length <= 3) return points;
        points = points.OrderBy(p => p.x).ThenBy(p => p.y).ToArray();

        List<Vector2> hull = new List<Vector2>();
        foreach (var p in points)
        {
            while (hull.Count >= 2 && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }

        int lowerHullSize = hull.Count;
        for (int i = points.Length - 2; i >= 0; i--)
        {
            Vector2 p = points[i];
            while (hull.Count > lowerHullSize && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }

        hull.RemoveAt(hull.Count - 1); // Remove last point since it's duplicate of first

        return hull.ToArray();
    }

    private float CrossProduct(Vector2 A, Vector2 B, Vector2 C)
    {
        return (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x);
    }

    private void OnValidate()
    {
        Enabled = diceProbability > 0f;
        if (!Enabled && diceDebugger != null)
        {
            DebuggerFactory.DestroyDiceDebugger(diceDebugger);
            diceDebugger = null;
        }
    }
}
