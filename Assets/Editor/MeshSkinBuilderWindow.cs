using System;
using UnityEngine;

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Object = UnityEngine.Object;

public class MeshSkinBuilderWindow : EditorWindow
{
    private MeshAssetList assets;
    private SerializedObject overridesSerialized;
    private Vector2 overridesScroll;

    [MenuItem("Tools/MeshSkinBuilder")]
    public static void ShowWindow()
    {
        GetWindow<MeshSkinBuilderWindow>("Bundle Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assets", EditorStyles.boldLabel);

        assets = (MeshAssetList)EditorGUILayout.ObjectField("Assets To Build", assets, typeof(MeshAssetList), false);

        GUILayout.Space(10);

        if (overridesSerialized == null || overridesSerialized.targetObject != assets)
            overridesSerialized = new SerializedObject(assets);

        overridesSerialized.Update();

        GUILayoutOption[] scrollOptions = new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MaxHeight(500) };
        overridesScroll = EditorGUILayout.BeginScrollView(overridesScroll, scrollOptions);
        SerializedProperty list = overridesSerialized.FindProperty("objects");
        EditorGUILayout.PropertyField(list, includeChildren: true);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        if (GUILayout.Button("Build AssetBundle"))
        {
            BuildBundle();
        }
        
        overridesSerialized.ApplyModifiedProperties();
    }

    private void BuildBundle()
    {
        if (!Directory.Exists(assets.outputPath))
            Directory.CreateDirectory(assets.outputPath);

        List<string> assetPaths = new List<string>();
        List<string> assetNames = new List<string>();

        foreach (var asset in assets.objects)
        {
            if (asset == null) continue;

            string path = AssetDatabase.GetAssetPath(asset);
            assetPaths.Add(path);
            
            string name = Path.GetFileNameWithoutExtension(path);
            assetNames.Add(name);
        }

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = assets.name,
            assetNames = assetPaths.ToArray(),
            addressableNames = assetNames.ToArray()
        };

        BuildPipeline.BuildAssetBundles(
            assets.outputPath,
            new AssetBundleBuild[] { build },
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget
        );

        Debug.Log($"Built single bundle '{assets.name}' with {assetPaths.Count} assets.");
    }
}
