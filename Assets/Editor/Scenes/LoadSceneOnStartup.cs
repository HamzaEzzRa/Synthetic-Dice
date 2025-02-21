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

            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            EditorWindow gameViewWindow = EditorWindow.GetWindow(gameViewType);

            var method = gameViewWindow.GetType().GetMethod(
                "SizeSelectionCallback",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            method?.Invoke(gameViewWindow, new object[] { "640x480" });

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
