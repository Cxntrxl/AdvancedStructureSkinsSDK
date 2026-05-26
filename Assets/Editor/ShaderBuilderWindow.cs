using System;
using System.Collections.Generic;
using System.IO;
using AdvancedStructureSkins.Shared.SDK;
using AdvancedStructureSkins.Shared.SDK.Binary;
using UnityEditor;
using UnityEngine;

public class ShaderBuilderWindow : EditorWindow
{
    private string bundleName = "custombundle";
    private string outputPath = "";
    
    private SkinManifest skinManifest;
    private Editor manifestEditor;
    
    private Vector2 scroll;

    [MenuItem("Tools/ASS Asset Bundle Builder")]
    public static void ShowWindow()
    {
        var w = GetWindow<ShaderBuilderWindow>("ASS Asset Bundle Builder");
        w.minSize = new Vector2(500, 500);
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(outputPath)) return;
        
        outputPath = EditorPrefs.HasKey("ASS_ASBOutputPath") 
            ? EditorPrefs.GetString("ASS_ASBOutputPath") 
            : Path.Combine(Application.dataPath, "AssetBundles");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Advanced Structure Skins Asset Bundle Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawSkinManifest();

        EditorGUILayout.EndScrollView();
        
        DrawBottomPanel();
    }

    private void DrawSkinManifest()
    {
        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("Skin Manifest", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel("Manifest");

        if (GUILayout.Button("Create", GUILayout.Width(80)))
        {
            CreateSkinManifestAsset();
        }
        
        skinManifest = (SkinManifest)EditorGUILayout.ObjectField(
            skinManifest,
            typeof(SkinManifest),
            false
        );

        EditorGUILayout.EndHorizontal();

        if (skinManifest == null)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        if (manifestEditor == null || manifestEditor.target != skinManifest)
        {
            DestroyImmediate(manifestEditor);
            manifestEditor = Editor.CreateEditor(skinManifest);
        }

        manifestEditor.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
    }
    
    private void DrawBottomPanel()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel("Output Path");

        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string chosen = EditorUtility.SaveFolderPanel("Output Folder", outputPath, "");
            if (!string.IsNullOrEmpty(chosen))
            {
                outputPath = chosen;
                EditorPrefs.SetString("ASS_ASBOutputPath", outputPath);
            }
        }
        
        outputPath = EditorGUILayout.TextField(outputPath);

        EditorGUILayout.EndHorizontal();
        
        bundleName = EditorGUILayout.TextField("File Name", bundleName);

        GUILayout.Space(5);

        if (GUILayout.Button("Build .asb", GUILayout.Height(40)))
        {
            Build();
        }

        GUILayout.Space(5);

        EditorGUILayout.HelpBox("Build AssetBundles for Advanced Structure Skins.\n" +
                                "Each bundle contains a SkinManifest which defines materials, overrides and texture sets.\n" +
                                "Hover over a given field for more information.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }
    
    private void CreateSkinManifestAsset()
    {
        SkinManifest asset = ScriptableObject.CreateInstance<SkinManifest>();

        string path = EditorUtility.SaveFilePanelInProject(
            "Create Skin Manifest",
            "NewSkinManifest",
            "asset",
            "Choose where to save the Skin Manifest asset."
        );

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        skinManifest = AssetDatabase.LoadAssetAtPath<SkinManifest>(path);

        if (skinManifest != null)
        {
            Selection.activeObject = skinManifest;
            EditorGUIUtility.PingObject(skinManifest);
        }
    }

    private void Build()
    {
        Directory.CreateDirectory(outputPath);

        string tempDir = "Assets/Temp";

        if (!Directory.Exists(tempDir))
            Directory.CreateDirectory(tempDir);
        
        byte[] manifestBytes = BinaryHandler.Write(skinManifest);

        string manifestPath = Path.Combine(tempDir, $"{skinManifest.skinName}_manifest.bytes");
        File.WriteAllBytes(manifestPath, manifestBytes);

        AssetDatabase.ImportAsset(manifestPath);

        TextAsset manifestAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);

        if (manifestAsset == null)
        {
            Debug.LogError("Failed to create manifest TextAsset.");
            return;
        }
        
        HashSet<Texture> texturesToInclude = new HashSet<Texture>();

        if (skinManifest.shaders != null)
        {
            foreach (ShaderManifest sm in skinManifest.shaders)
            {
                if (sm.overrides != null)
                {
                    foreach (var o in sm.overrides)
                    {
                        if (o.textureValue != null)
                            texturesToInclude.Add(o.textureValue);
                    }
                }
            }
        }

        if (skinManifest.textures != null)
        {
            foreach (var set in skinManifest.textures)
            {
                if (set.textures == null) continue;

                if (set.previewTexture != null)
                    texturesToInclude.Add(set.previewTexture);
                
                foreach (var entry in set.textures)
                {
                    if (entry?.textures == null) continue;

                    foreach (var tex in entry.textures)
                    {
                        if (tex != null)
                            texturesToInclude.Add(tex);
                    }
                }
            }
        }
        
        if (skinManifest.previewTexture != null)
            texturesToInclude.Add(skinManifest.previewTexture);

        var assetPaths = new List<string>
        {
            manifestPath
        };

        var addressableNames = new List<string>
        {
            "manifest"
        };

        foreach (var tex in texturesToInclude)
        {
            string path = AssetDatabase.GetAssetPath(tex);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Texture '{tex.name}' is not an asset and will be skipped.");
                continue;
            }

            assetPaths.Add(path);
            addressableNames.Add(tex.name);
        }

        if (skinManifest.shaders != null)
        {
            foreach (ShaderManifest sm in skinManifest.shaders)
            {
                assetPaths.Add(AssetDatabase.GetAssetPath(sm.material));
                addressableNames.Add(sm.material.name);
            }
        }

        var build = new AssetBundleBuild
        {
            assetBundleName = bundleName + ".asb",
            assetNames = assetPaths.ToArray(),
            addressableNames = addressableNames.ToArray()
        };

        try
        {
            EditorUtility.DisplayProgressBar("Building ASB", "Building asset bundle...", 0.3f);

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
                "ASB built successfully:\n" + outputPath,
                "OK"
            );
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Build Failed", e.Message, "OK");
        }
    }

    private void CleanupExtraBuildFiles()
    {
        foreach (var file in Directory.GetFiles(outputPath))
        {
            if (file.EndsWith(".manifest") || Path.GetFileName(file) == Path.GetFileName(outputPath))
                File.Delete(file);
        }
    }
}