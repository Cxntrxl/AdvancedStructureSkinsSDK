using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewSnapshotTool
{
    private static bool _active;

    static SceneViewSnapshotTool()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    [MenuItem("Tools/Capture 90x90 Scene Snapshot")]
    public static void StartCapture()
    {
        _active = true;
        SceneView.RepaintAll();
    }

    private static void DuringSceneGUI(SceneView sceneView)
    {
        if (!_active)
            return;

        Handles.BeginGUI();

        Rect sceneRect = sceneView.position;
        
        float size = sceneRect.height * 0.7f;
        
        Rect captureRect = new Rect(
            (sceneRect.width - size) * 0.5f,
            (sceneRect.height - size) * 0.5f,
            size,
            size
        );

        Color old = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);

        GUI.DrawTexture(new Rect(0, 0, sceneRect.width, captureRect.y), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, captureRect.yMax, sceneRect.width, sceneRect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, captureRect.y, captureRect.x, captureRect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(captureRect.xMax, captureRect.y, sceneRect.width, captureRect.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
        
        Handles.DrawSolidRectangleWithOutline(
            captureRect,
            new Color(0, 0, 0, 0),
            Color.white
        );
        
        GUILayout.BeginArea(new Rect(10, 10, 200, 80));

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Capture", GUILayout.Height(30)))
        {
            Capture(sceneView, captureRect);
            _active = false;
        }

        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            _active = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        GUI.color = old;

        Handles.EndGUI();

        sceneView.Repaint();
    }

    private static void Capture(SceneView sceneView, Rect captureRect)
    {
        Camera sceneCam = sceneView.camera;

        const int outputSize = 90;

        RenderTexture rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);

        GameObject tempCamGO = new GameObject("TempSceneCaptureCamera");
        tempCamGO.hideFlags = HideFlags.HideAndDontSave;

        Camera cam = tempCamGO.AddComponent<Camera>();

        cam.CopyFrom(sceneCam);

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);

        Skybox skybox = cam.GetComponent<Skybox>();
        if (skybox != null)
            skybox.enabled = false;

        cam.targetTexture = rt;

        cam.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D full = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        full.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        full.Apply();

        RenderTexture.active = previous;
        
        Rect sceneRect = sceneView.position;

        float normalizedX = captureRect.x / sceneRect.width;
        float normalizedY = captureRect.y / sceneRect.height;
        float normalizedSize = captureRect.width / sceneRect.width;

        int px = Mathf.RoundToInt(normalizedX * full.width);
        int py = Mathf.RoundToInt((1f - normalizedY - normalizedSize) * full.height);
        int psize = Mathf.RoundToInt(normalizedSize * full.width);

        Texture2D cropped = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);

        Color[] pixels = full.GetPixels(px, py, psize, psize);

        Texture2D temp = new Texture2D(psize, psize, TextureFormat.RGBA32, false);
        temp.SetPixels(pixels);
        temp.Apply();
        
        RenderTexture scaleRT = RenderTexture.GetTemporary(outputSize, outputSize);

        Graphics.Blit(temp, scaleRT);

        RenderTexture.active = scaleRT;

        cropped.ReadPixels(new Rect(0, 0, outputSize, outputSize), 0, 0);
        cropped.Apply();

        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(scaleRT);

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Snapshot",
            "SceneSnapshot",
            "png",
            "Save snapshot"
        );

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, cropped.EncodeToPNG());
            AssetDatabase.Refresh();

            Debug.Log($"Saved snapshot to: {path}");
        }

        Object.DestroyImmediate(tempCamGO);
        Object.DestroyImmediate(full);
        Object.DestroyImmediate(temp);
        Object.DestroyImmediate(cropped);

        rt.Release();
        Object.DestroyImmediate(rt);
    }
}