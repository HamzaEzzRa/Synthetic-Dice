using UnityEngine;

using System.IO;
using System.Collections;
using System.Collections.Generic;

using Unity.EditorCoroutines.Editor;

using Newtonsoft.Json;

[System.Serializable]
public struct LabelData
{
    public int value;
    public SimpleRect BBox;
    public float angle;

    public LabelData(LabelData source)
    {
        value = source.value;
        BBox = source.BBox;
        angle = source.angle;
    }

    public LabelData(int value, SimpleRect BBox)
    {
        this.value = value;
        this.BBox = BBox;
        this.angle = 0f;
    }

    public LabelData(int value, SimpleRect BBox, float angle)
    {
        this.value = value;
        this.BBox = BBox;
        this.angle = angle;
    }
}

public class DatasetGenerator : MonoBehaviour
{
    public enum ImageEncoding
    {
        PNG,
        JPEG
    }

    public enum BBoxType
    {
        AXIS_ALIGNED,
        ORIENTED,
    }

    [SerializeField] private Camera renderCamera;
    [SerializeField] PoseGenerator poseGenerator;

    [SerializeField] private LayerMask cullingLayerMask;

    [SerializeField] private DiceRandomizer[] dices;

    [Header("Settings")]
    [SerializeField] private int width = 640;
    [SerializeField] private int height = 480;
    [SerializeField, Range(0f, 1f)] private float minimumDiceSurface = 0.5f;

    [SerializeField] private ImageEncoding imageEncoding;
    [SerializeField, Min(1)] private int datasetSize = 10000;

    [SerializeField] private bool yoloFormat = false;
    [SerializeField] private BBoxType boundingBoxType = BBoxType.AXIS_ALIGNED;

    public EditorCoroutine CurrentCoroutine => currentCoroutine;
    public float CoroutineProgress { get; private set; }

    private int depth = 16;

    private Texture2D texture;
    private byte[] image;

    [HideInInspector, SerializeField] private EditorCoroutine currentCoroutine;

    public void Generate()
    {
        Stop();

        currentCoroutine = EditorCoroutineUtility.StartCoroutine(GenerateCoroutine(), this);
    }

    public void Stop()
    {
        if (currentCoroutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }

    private void Capture()
    {
        if (texture != null)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(texture);
            }
            else
            {
                Destroy(texture);
            }
        }

        texture = null;
        image = null;

        RenderTexture renderTexture = new RenderTexture(width, height, depth);
        renderCamera.targetTexture = renderTexture;
        CameraClearFlags clearFlags = renderCamera.clearFlags;
        int cullingMask = renderCamera.cullingMask;

        renderCamera.cullingMask = cullingLayerMask;

        // Transparency
        if (imageEncoding == ImageEncoding.PNG)
        {
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
        }

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        renderCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);

        if (imageEncoding == ImageEncoding.PNG)
        {
            image = texture.EncodeToPNG();
        }
        else if (imageEncoding == ImageEncoding.JPEG)
        {
            image = texture.EncodeToJPG();
        }

        renderCamera.targetTexture = null;
        RenderTexture.active = null;
        renderCamera.clearFlags = clearFlags;
        renderCamera.cullingMask = cullingMask;

        if (Application.isEditor)
        {
            DestroyImmediate(renderTexture);
        }
        else
        {
            Destroy(renderTexture);
        }
    }

    private IEnumerator GenerateCoroutine()
    {
        string datasetPath = Path.Combine(new string[] {
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "datasets",
            "synth-dice-" + datasetSize.ToString() +
            "-" + boundingBoxType.ToString() + "_" +
            System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")
        });
        if (!Directory.Exists(datasetPath))
        {
            Directory.CreateDirectory(datasetPath);
        }

        SimpleRect imageRect = new SimpleRect(0, 0, width, height);
        Dictionary<string, LabelData[]> labelDictionary = new Dictionary<string, LabelData[]>();

        int i = 0;
        CoroutineProgress = 0f;
        while (i < datasetSize)
        {
            poseGenerator.Generate();

            yield return new WaitForFixedUpdate();

            string ext = ".png";
            if (imageEncoding == ImageEncoding.JPEG)
            {
                ext = ".jpg";
            }

            string filename = "dice_" + System.DateTime.Now.ToString("HH-mm-ss-fff") + '_' + i.ToString() + ext;
            List<LabelData> labelDataList = new List<LabelData>(dices.Length);
            for (int j = 0; j < dices.Length; j++)
            {
                if (imageRect.Contains(dices[j].BoundingRect, minimumDiceSurface))
                {
                    // Unity screen-space (0, 0) coordinate is bottom-left, OpenCV is top-left
                    if (boundingBoxType == BBoxType.AXIS_ALIGNED)
                    {
                        SimpleRect rect = dices[j].BoundingRect;
                        SimpleRect yFlippedRect = new SimpleRect(
                            rect.xMin,
                            height - rect.yMax,
                            rect.xMax,
                            height - rect.yMin
                        );
                        labelDataList.Add(new LabelData(dices[j].CurrentValue, yFlippedRect));
                    }
                    else if (boundingBoxType == BBoxType.ORIENTED)
                    {
                        AngledRect angledRect = dices[j].AngledBoundingRect;
                        SimpleRect yFlippedRect = new SimpleRect(
                            angledRect.Rect.xMin,
                            height - angledRect.Rect.yMax,
                            angledRect.Rect.xMax,
                            height - angledRect.Rect.yMin
                        );
                        labelDataList.Add(new LabelData(dices[j].CurrentValue, yFlippedRect, angledRect.Angle));
                    }
                }
            }

            int activeDiceCount = 0;
            for (int j = 0; j < dices.Length; j++)
            {
                if (dices[j].Enabled)
                {
                    activeDiceCount++;
                }
            }

            if (activeDiceCount > 0 && labelDataList.Count == 0)
            {
                Debug.LogWarning("No visible dice in generated image, skipping...");
                continue;
            }

            LabelData[] labelDataArray = labelDataList.ToArray();
            labelDictionary.Add(filename, labelDataArray);

            Capture();

            string label = GetTotalDiceValue(labelDataArray).ToString();
            //Debug.Log(label);

            string folderPath = Path.Combine(datasetPath, label);
            if (yoloFormat)
            {
                folderPath = Path.Combine(datasetPath, "images");
            }
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, filename);
            //Debug.Log(path);

            byte[] copy = new byte[image.Length];
            image.CopyTo(copy, 0);
            File.WriteAllBytesAsync(filePath, copy).ContinueWith((task) => {
                copy = null;
            });

            i++;
            CoroutineProgress = (float)i / datasetSize;
        }

        if (yoloFormat)
        {
            string txtPath = Path.Combine(datasetPath, "labels");
            if (!Directory.Exists(txtPath))
            {
                Directory.CreateDirectory(txtPath);
            }
            foreach (var pair in labelDictionary)
            {
                string txtFilename = Path.ChangeExtension(pair.Key, ".txt");
                string txtFilePath = Path.Combine(txtPath, txtFilename);
                using (StreamWriter writer = File.CreateText(txtFilePath))
                {
                    LabelData[] labelDataArray = pair.Value;
                    for (int j = 0; j < labelDataArray.Length; j++)
                    {
                        LabelData labelData = labelDataArray[j];
                        Vector2 min = new Vector2(Mathf.Max(0f, labelData.BBox.xMin / width), Mathf.Max(0f, labelData.BBox.yMin / height));
                        Vector2 max = new Vector2(Mathf.Min(1f, labelData.BBox.xMax / width), Mathf.Min(1f, labelData.BBox.yMax / height));
                        float angle = labelData.angle;

                        // YOLO label: <class_idx> <x_center> <y_center> <width> <height> [<angle>]
                        string labelLine = $"{labelData.value - 1} {(min.x + max.x) / 2f} {(min.y + max.y) / 2f} {max.x - min.x} {max.y - min.y}";
                        if (boundingBoxType == BBoxType.ORIENTED)
                        {
                            labelLine += $" {angle}";
                        }
                        writer.WriteLine(labelLine);
                    }
                }
            }
        }
        else
        {
            string jsonPath = Path.Combine(datasetPath, "labels.json");
            DumpAsJson(labelDictionary, jsonPath);
        }

        currentCoroutine = null;
    }

    private int GetTotalDiceValue(LabelData[] labelDataArray)
    {
        int totalValue = 0;
        for (int i = 0; i < labelDataArray.Length; i++)
        {
            totalValue += labelDataArray[i].value;
        }

        return totalValue;
    }

    private void DumpAsJson(object data, string filePath = "./dataset.json")
    {
        if (!File.Exists(filePath))
        {
            using (StreamWriter writer = File.CreateText(filePath))
            {
                writer.WriteLine(JsonConvert.SerializeObject(data));
            }
        }
        else
        {
            var jsonData = JsonConvert.SerializeObject(data);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Seek(-3, SeekOrigin.End);
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(",");
                    writer.WriteLine(jsonData.Substring(1, jsonData.Length - 1));
                }
            }
        }
    }
}
