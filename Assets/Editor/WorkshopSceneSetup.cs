using AnchorMaster.Workshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class WorkshopSceneSetup
{
    [MenuItem("AnchorMaster/Create Playable Workshop Scene")]
    public static void CreateScene()
    {
        CreateLevelAssets();
        const string scenePath = "Assets/Scenes/AnchorWorkshop.unity";
        if (File.Exists(scenePath))
        {
            AddBuildEntry(scenePath);
            Debug.Log("Anchor Workshop scene already exists; leaving it unchanged.");
            return;
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().orthographic = true;
        var starter = new GameObject("Workshop Bootstrap");
        starter.AddComponent<AnchorWorkshopBootstrap>();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), scenePath);
        AddBuildEntry(scenePath);
        Debug.Log("Anchor Workshop playable scene created and added first to Build Settings.");
    }

    static void AddBuildEntry(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var scene in EditorBuildSettings.scenes) if (scene.path != scenePath) scenes.Add(scene);
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void CreateLevelAssets()
    {
        const string folder = "Assets/Resources/WorkshopLevels";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "WorkshopLevels");
        for (int i = 0; i < 5; i++)
        {
            string path = folder + "/Workshop_" + (i + 1) + ".asset";
            if (File.Exists(path)) continue;
            AssetDatabase.CreateAsset(WorkshopLevel.CreateDefault(i), path);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
