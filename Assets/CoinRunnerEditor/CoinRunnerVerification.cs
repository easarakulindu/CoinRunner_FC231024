using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CoinRunnerVerification
{
    public static void RepairTextAndPreview()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        // Keep the optional platform above the normal gap-jump approach.
        GameObject.Find("MovingPlatform").transform.position = new Vector3(8, 2.5f, 0);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("TMP Essentials must be imported before assigning text.");
        foreach (var root in scene.GetRootGameObjects())
            foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(text);
            }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        // Preview only: these temporary camera/UI changes are never saved into the game scene.
        var camera = Camera.main;
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Capture(camera, "Logs/CoinRunner-start.png");
        camera.transform.position = new Vector3(6.5f, -1, -10);
        camera.orthographicSize = 10;
        Capture(camera, "Logs/CoinRunner-level.png");
        camera.orthographicSize = 5;
        camera.transform.position = new Vector3(20, -1, -10);
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        manager.winPanel.SetActive(true);
        Capture(camera, "Logs/CoinRunner-win.png");
        Debug.Log("Text references verified and three scene previews captured.");
    }

    private static void Capture(Camera camera, string path)
    {
        var target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;
        camera.aspect = 16f / 9f;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
    }
}
