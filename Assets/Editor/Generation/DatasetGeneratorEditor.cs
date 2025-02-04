using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DatasetGenerator))]
public class DatasetGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DatasetGenerator generator = (DatasetGenerator)target;

        GUILayout.Space(16);

        if (GUILayout.Button(new GUIContent("Generate", "Generate a dataset of images with randomized poses"), GUILayout.Height(25f)))
        {
            generator.Generate();
        }

        GUILayout.Space(4);

        if (generator.CurrentCoroutine != null)
        {
            if (GUILayout.Button(new GUIContent("Stop", "Stop the ongoing generation"), GUILayout.Height(25f)))
            {
                generator.Stop();
            }
        }
    }
}
