using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ShaderBuilderWindow : EditorWindow
{
    private string bundleName = "custombundle";
    private string outputPath = "";
    private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
    private Shader shader;
    private Material material;
    private Editor materialEditor;
    private MaterialPropertyOverrides overridesAsset;
    private SerializedObject overridesSerialized;
    private Vector2 overridesScroll;
    
    [MenuItem("Tools/Shader Asset Bundle Builder")]
    public static void ShowWindow()
    {
        var w = GetWindow<ShaderBuilderWindow>("Shader Asset Bundle Builder");
        w.minSize = new Vector2(500, 500);
    }
    
    private void OnEnable()
    {
        buildTarget = EditorUserBuildSettings.activeBuildTarget;
        if (string.IsNullOrEmpty(outputPath))
            outputPath = System.IO.Path.Combine(Application.dataPath, "../AssetBundles");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Advanced Structure Skins Asset Bundle Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Shader", EditorStyles.label);
        shader = (Shader)EditorGUILayout.ObjectField(shader, typeof(Shader), false);

        if (shader == null)
        {
            EditorGUILayout.HelpBox("Please assign a shader for your skin.", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Material", EditorStyles.label);
        material = (Material)EditorGUILayout.ObjectField(material, typeof(Material), false);
        
        if (material == null)
        {
            EditorGUILayout.HelpBox("Please assign a material for your skin.", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Material Preview", EditorStyles.label);
        if (material != null)
        {
            if (materialEditor == null || materialEditor.target != material)
            {
                DestroyImmediate(materialEditor);
                materialEditor = Editor.CreateEditor(material);
            }
            
            materialEditor.OnInspectorGUI();

            GUILayout.Space(10);

            Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(true));
            materialEditor.OnPreviewGUI(previewRect, EditorStyles.helpBox);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        DrawOverrides();
        EditorGUILayout.Space();

        bundleName = EditorGUILayout.TextField("Skin Name", bundleName);
        
        // I removed the buildTarget functionality from the shader builder to make things
        // more accessible to 90% of players.
        // If the StandaloneWindows64 does not work for you (potentially linux users?),
        // you can un-comment this code, and it'll add a build target field to the builder.
        
        /*EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build Target", GUILayout.Width(80));
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(buildTarget);
        EditorGUILayout.EndHorizontal();*/
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Output Path", GUILayout.Width(80));
        EditorGUILayout.SelectableLabel(outputPath, GUILayout.Height(16));

        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string chosen = EditorUtility.SaveFolderPanel("Choose Output Folder for Shader Assetbundle", outputPath, "");
            if (!string.IsNullOrEmpty(chosen)) outputPath = chosen;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (GUILayout.Button("Build", GUILayout.Height(40)))
        {
            if (shader == null || material == null)
            {
                EditorUtility.DisplayDialog("Assign Assets", "Assign all assets before building.", "OK");
            }
            else if (string.IsNullOrEmpty(outputPath))
            {
                EditorUtility.DisplayDialog("Output Folder", "Please choose an output folder.", "OK");
            }
            else
            {
                Build();
            }
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("Build AssetBundles for your skins using this window. All shader bundles require a shader and a material reference to be fully compatible with Advanced Structure Skins or other mods depending on CustomShaders.", MessageType.Info);
    }

    private void DrawOverrides()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Property Overrides", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        overridesAsset = (MaterialPropertyOverrides)EditorGUILayout.ObjectField(
            "Overrides Asset", overridesAsset, typeof(MaterialPropertyOverrides), false);

        if (overridesAsset == null)
        {
            if (GUILayout.Button("Create"))
            {
                overridesAsset = CreateInstance<MaterialPropertyOverrides>();
                string path = EditorUtility.SaveFilePanelInProject("Save Overrides Asset", "Overrides", "asset",
                    "Save your overrides asset.");
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(overridesAsset, path);
                    AssetDatabase.SaveAssets();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        } else { EditorGUILayout.EndHorizontal(); }
        
        if (overridesSerialized == null || overridesSerialized.targetObject != overridesAsset)
            overridesSerialized = new SerializedObject(overridesAsset);

        overridesSerialized.Update();

        GUILayoutOption[] scrollOptions = new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.MaxHeight(500) };
        overridesScroll = EditorGUILayout.BeginScrollView(overridesScroll, scrollOptions);
        SerializedProperty list = overridesSerialized.FindProperty("overrides");
        EditorGUILayout.PropertyField(list, includeChildren: true);
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Override"))
        {
            overridesAsset.overrides.Add(new MaterialPropertyOverride());
        }

        if (GUILayout.Button("Remove All Overrides"))
        {
            if (EditorUtility.DisplayDialog("Clear Overrides",
                    "Are you sure you want to remove all property overrides? \nIf your asset is not built, this cannot be undone.",
                    "Yes", "No"))
                overridesAsset.overrides.Clear();
        }

        overridesSerialized.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
    }
    
    private void ExportOverridesData(MaterialPropertyOverrides so, string path)
    {
        string data = "";
        foreach (MaterialPropertyOverride o in so.overrides)
        {
            data += o.propertyName + "|";
            data += (int)o.propertyType + "|";
            switch (o.propertyType)
            {
                case ShaderPropertyType.Color:
                    data += o.colorValue.r + "|";
                    data += o.colorValue.g + "|";
                    data += o.colorValue.b + "|";
                    data += o.colorValue.a + "|";
                    break;
                case ShaderPropertyType.Float:
                    data += o.floatValue + "|";
                    break;
                case ShaderPropertyType.Range:
                    data += o.floatValue + "|";
                    break;
                case ShaderPropertyType.Int:
                    data += o.intValue + "|";
                    break;
                case ShaderPropertyType.Vector:
                    data += o.vectorValue.x + "|";
                    data += o.vectorValue.y + "|";
                    data += o.vectorValue.z + "|";
                    data += o.vectorValue.w + "|";
                    break;
            }

            data += o.targetStructures.Count + "|";
            
            foreach (StructureType type in o.targetStructures)
            {
                data += (int)type + "|";
            }
        }

        File.WriteAllText(Path.Combine(Application.dataPath, "Temp/structure_overrides.txt"), data);
    }
    
    private void Build()
    {
        System.IO.Directory.CreateDirectory(outputPath);

        AssetBundleBuild[] builds = new AssetBundleBuild[1];
        
        List<string> assetPaths = new List<string>();
        List<string> addressableNames = new List<string>();

        assetPaths.Add(AssetDatabase.GetAssetPath(shader));
        addressableNames.Add("shader");
        
        assetPaths.Add(AssetDatabase.GetAssetPath(material));
        addressableNames.Add("material");
        
        if (overridesAsset != null)
        {
            string dataPath = "Temp/structure_overrides.txt";
            ExportOverridesData(overridesAsset, dataPath);
            AssetDatabase.ImportAsset(Path.Combine("Assets", dataPath));
            
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine("Assets", dataPath));
            
            assetPaths.Add(AssetDatabase.GetAssetPath(textAsset));
            addressableNames.Add("overrides");
        }
        
        var abb = new AssetBundleBuild
        {
            assetBundleName = bundleName + ".bundle",
            assetNames = assetPaths.ToArray(),
            addressableNames = addressableNames.ToArray()
        };
        builds[0] = abb;

        try
        {
            EditorUtility.DisplayProgressBar("Building AssetBundles", "Preparing...", 0.1f);

            BuildPipeline.BuildAssetBundles(outputPath, builds, BuildAssetBundleOptions.ForceRebuildAssetBundle, buildTarget);
            CleanupExtraBuildFiles();
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Build Complete", "AssetBundles built to:\n" + outputPath, "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Build Failed", ex.Message, "OK");
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
