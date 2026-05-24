using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UIFBundleBuilderWindow : EditorWindow
{
    [Serializable]
    public class BundlePrefabEntry
    {
        public GameObject prefab;
        public string addressableName = "";
    }

    private readonly List<BundlePrefabEntry> prefabs = new();

    private string bundleName = "custombundle";
    private string outputPath = "";

    private Vector2 scroll;

    [MenuItem("Tools/UIFramework Custom Element Bundle Builder")]
    public static void ShowWindow()
    {
        var w = GetWindow<UIFBundleBuilderWindow>("UIFramework Custom Element Bundle Builder");
        w.minSize = new Vector2(500, 500);
    }

    private void OnEnable()
    {
        outputPath = EditorPrefs.HasKey("UIF_BundleOutputPath")
            ? EditorPrefs.GetString("UIF_BundleOutputPath")
            : Path.Combine(Application.dataPath, "AssetBundles");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "UIFramework Custom Element Bundle Builder",
            EditorStyles.boldLabel);

        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawPrefabList();

        GUILayout.Space(10);

        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();

        DrawBottomPanel();
    }

    private void DrawPrefabList()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

        GUILayout.Space(5);

        for (int i = 0; i < prefabs.Count; i++)
        {
            var entry = prefabs[i];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Element {i + 1}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                prefabs.RemoveAt(i);
                GUI.backgroundColor = Color.white;
                return;
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            entry.prefab = (GameObject)EditorGUILayout.ObjectField(
                "Prefab",
                entry.prefab,
                typeof(GameObject),
                false
            );

            entry.addressableName = EditorGUILayout.TextField(
                "Addressable Name",
                entry.addressableName
            );

            if (entry.prefab != null && string.IsNullOrWhiteSpace(entry.addressableName))
            {
                entry.addressableName = entry.prefab.name.ToLowerInvariant();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Add Prefab", GUILayout.Height(30)))
        {
            prefabs.Add(new BundlePrefabEntry());
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBottomPanel()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel("Output Path");

        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string chosen = EditorUtility.SaveFolderPanel(
                "Output Folder",
                outputPath,
                ""
            );

            if (!string.IsNullOrEmpty(chosen))
            {
                outputPath = chosen;
                EditorPrefs.SetString("UIF_BundleOutputPath", outputPath);
            }
        }

        outputPath = EditorGUILayout.TextField(outputPath);

        EditorGUILayout.EndHorizontal();

        bundleName = EditorGUILayout.TextField("Bundle Name", bundleName);

        GUILayout.Space(10);

        GUI.enabled = prefabs.Count > 0;

        if (GUILayout.Button("Build AssetBundle", GUILayout.Height(40)))
        {
            Build();
        }

        GUI.enabled = true;

        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "I use this to build UIFramework prefab assetbundles for developing Advanced Structure Skins. You shouldn't need this window for developing skins - Cxntrxl",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    private void Build()
    {
        Directory.CreateDirectory(outputPath);

        List<string> assetPaths = new();
        List<string> addressableNames = new();

        foreach (var entry in prefabs)
        {
            if (entry.prefab == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.addressableName))
            {
                Debug.LogWarning($"Prefab '{entry.prefab.name}' has no addressable name.");
                continue;
            }

            string path = AssetDatabase.GetAssetPath(entry.prefab);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Could not get asset path for '{entry.prefab.name}'.");
                continue;
            }

            assetPaths.Add(path);
            addressableNames.Add(entry.addressableName);
        }

        if (assetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Nothing To Build",
                "No valid prefabs were added.",
                "OK"
            );

            return;
        }

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = bundleName + ".bundle",
            assetNames = assetPaths.ToArray(),
            addressableNames = addressableNames.ToArray()
        };

        try
        {
            EditorUtility.DisplayProgressBar(
                "Building Bundle",
                "Building AssetBundle...",
                0.5f
            );

            BuildPipeline.BuildAssetBundles(
                outputPath,
                new[] { build },
                BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneWindows64
            );

            CleanupExtraBuildFiles();

            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Build Complete",
                $"Bundle built successfully:\n{outputPath}",
                "OK"
            );
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();

            Debug.LogException(e);

            EditorUtility.DisplayDialog(
                "Build Failed",
                e.Message,
                "OK"
            );
        }
    }

    private void CleanupExtraBuildFiles()
    {
        foreach (var file in Directory.GetFiles(outputPath))
        {
            if (file.EndsWith(".manifest") ||
                Path.GetFileName(file) == Path.GetFileName(outputPath))
            {
                File.Delete(file);
            }
        }
    }
}