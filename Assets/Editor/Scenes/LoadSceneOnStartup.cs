using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LoadSceneOnStartup
{
    private const string SceneToLoad = "Assets/Scenes/Generation Scene.unity";
    private const string LayoutFile = "Assets/Editor/Layouts/simulator_layout.wlt";

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += () =>
        {
            EditorSceneManager.OpenScene(SceneToLoad);

            if (System.IO.File.Exists(LayoutFile))
            {
                EditorUtility.LoadWindowLayout(LayoutFile);
            }
            else
            {
                Debug.LogWarning("Layout file not found: " + LayoutFile);
            }
        };
    }
}
