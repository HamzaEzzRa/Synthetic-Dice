using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseGenerator))]
public class PoseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(16);

        if (GUILayout.Button(new GUIContent("Randomize", "Randomize pose using the specified randomizers"), GUILayout.Height(25f)))
        {
            PoseGenerator generator = (PoseGenerator)target;
            generator.Generate();
        }
    }
}
