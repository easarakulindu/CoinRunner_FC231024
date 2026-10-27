using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Creates real, editable scene objects and assets, rather than generating the level at runtime.
public static class CoinRunnerSetup
{
    [Serializable]
    public class Configuration
    {
        public Color skyColor;
        public Color groundColor;
        public Vector2[] spikePositions;
        public string productName;
    }

    public static void Build()
    {
        const string configPath = "ProjectSettings/CoinRunnerSetup.json";
        if (!File.Exists(configPath))
            throw new InvalidOperationException("User choices must be recorded in " + configPath + " before setup.");
        var config = JsonUtility.FromJson<Configuration>(File.ReadAllText(configPath));
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        if (GameObject.Find("Player") != null)
            throw new InvalidOperationException("The scene already contains a Player. Setup will not overwrite it.");

        foreach (string folder in new[] { "Scripts", "Prefabs", "Tiles", "Palettes" })
            Directory.CreateDirectory("Assets/" + folder);
        AssetDatabase.Refresh();
        ConfigureTags();
        ImportTMP();

        Sprite square = ImportShape("Square", "Assets/Tiles/GroundSprite.png");
        Sprite capsule = ImportShape("Capsule", "Assets/Tiles/PlayerSprite.png");
        Sprite circle = ImportShape("Circle", "Assets/Tiles/CoinSprite.png");
        Sprite triangle = ImportShape("Triangle", "Assets/Tiles/SpikeSprite.png");
        var groundTile = ScriptableObject.CreateInstance<Tile>();
        groundTile.sprite = square;
        groundTile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(groundTile, "Assets/Tiles/GroundTile.asset");

        var grid = new GameObject("Grid", typeof(Grid));
        var ground = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        ground.transform.SetParent(grid.transform, false);
        ground.layer = LayerMask.NameToLayer("Ground");
        var map = ground.GetComponent<Tilemap>();
        map.color = config.groundColor;
        for (int x = -10; x <= 22; x++)
            if (x < 7 || x > 8)
                for (int y = -5; y <= -4; y++) map.SetTile(new Vector3Int(x, y), groundTile);
        for (int x = 2; x <= 5; x++) map.SetTile(new Vector3Int(x, -1), groundTile);
        for (int x = 12; x <= 15; x++) map.SetTile(new Vector3Int(x, 1), groundTile);
        for (int y = -3; y <= 2; y++) map.SetTile(new Vector3Int(-10, y), groundTile);
        var palette = GridPaletteUtility.CreateNewPalette("Assets/Palettes", "LevelPalette",
            GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var paletteContents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(palette));
        paletteContents.GetComponentInChildren<Tilemap>().SetTile(Vector3Int.zero, groundTile);
        PrefabUtility.SaveAsPrefabAsset(paletteContents, AssetDatabase.GetAssetPath(palette));
        PrefabUtility.UnloadPrefabContents(paletteContents);

        var player = MakeSprite("Player", capsule, Color.green, new Vector2(-6, -2), Vector3.one);
        player.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(1, 2);
        var noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0, bounciness = 0 };
        AssetDatabase.CreateAsset(noFriction, "Assets/Tiles/NoFriction.physicsMaterial2D");
        collider.sharedMaterial = noFriction;
        var body = player.AddComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var feet = new GameObject("GroundCheck").transform;
        feet.SetParent(player.transform, false);
        feet.localPosition = new Vector3(0, -1, 0);
        var movement = player.AddComponent<PlayerMovement>();
        movement.groundCheck = feet;
        movement.groundLayer = LayerMask.GetMask("Ground");
        player.AddComponent<PlayerInteractions>();
        var eye = MakeSprite("Eye", circle, Color.black, Vector2.zero, new Vector3(.14f, .14f, 1));
        eye.transform.SetParent(player.transform, false);
        eye.transform.localPosition = new Vector3(.23f, .4f, -.01f);
        eye.GetComponent<SpriteRenderer>().sortingOrder = 3;
        movement.facingMarker = eye.transform;

        var platform = MakeSprite("MovingPlatform", square, config.groundColor, new Vector2(8, 2.5f), new Vector3(2, .4f, 1));
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.AddComponent<BoxCollider2D>();
        platform.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        platform.AddComponent<MovingPlatform>();

        var camera = Camera.main;
        camera.backgroundColor = config.skyColor;
        camera.orthographic = true;
        camera.transform.position = player.transform.position + new Vector3(0, 1, -10);
        camera.gameObject.AddComponent<CameraFollow>().target = player.transform;

        var coin = MakeSprite("Coin", circle, Color.yellow, Vector2.zero, new Vector3(.5f, .5f, 1));
        coin.tag = "Coin";
        coin.AddComponent<CircleCollider2D>().isTrigger = true;
        var coinPrefab = PrefabUtility.SaveAsPrefabAsset(coin, "Assets/Prefabs/Coin.prefab");
        UnityEngine.Object.DestroyImmediate(coin);
        foreach (var position in new[] { new Vector2(3, 0), new Vector2(4, 0), new Vector2(7.5f, -2),
                     new Vector2(13, 2), new Vector2(14, 2), new Vector2(18, -3) })
            ((GameObject)PrefabUtility.InstantiatePrefab(coinPrefab)).transform.position = position;

        var spike = MakeSprite("Spike", triangle, Color.red, Vector2.zero, Vector3.one);
        spike.tag = "Hazard";
        spike.AddComponent<PolygonCollider2D>();
        var spikePrefab = PrefabUtility.SaveAsPrefabAsset(spike, "Assets/Prefabs/Spike.prefab");
        UnityEngine.Object.DestroyImmediate(spike);
        foreach (var position in config.spikePositions)
            ((GameObject)PrefabUtility.InstantiatePrefab(spikePrefab)).transform.position = position;

        var goal = MakeSprite("Goal", square, Color.white, new Vector2(21, -2), new Vector3(.3f, 2, 1));
        goal.tag = "Goal";
        goal.AddComponent<BoxCollider2D>().isTrigger = true;

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var coinText = MakeText("CoinText", canvas.transform, "Coins: 0 / 6", 50, new Vector2(500, 80), Color.black);
        var rect = coinText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -20);
        coinText.alignment = TextAlignmentOptions.TopLeft;
        manager.coinText = coinText;

        var panel = new GameObject("WinPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 200f / 255f);
        var title = MakeText("LevelCompleteText", panel.transform, "LEVEL COMPLETE!", 110, new Vector2(1400, 200), Color.white);
        title.rectTransform.anchoredPosition = new Vector2(0, 120);
        var buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 100);
        rect.anchoredPosition = new Vector2(0, -100);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        MakeText("Text (TMP)", button.transform, "Play Again", 50, new Vector2(400, 100), Color.black);
        UnityEventTools.AddPersistentListener(button.onClick, manager.RestartGame);
        manager.winPanel = panel;
        panel.SetActive(false);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
        PlayerSettings.productName = config.productName;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        // Set after building, because changing input handling may require an editor restart.
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        settings.FindProperty("activeInputHandler").intValue = 2;
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log("Coin Runner setup complete: SampleScene saved with the lab level, prefabs, palette and UI.");
    }

    private static void ImportTMP()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null) return;
        string package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).Single();
        UnityEditor.AssetPackage.Package.Import(package, false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") == null)
            throw new InvalidOperationException("Finish importing TMP Essentials before running scene setup again.");
    }

    private static Sprite ImportShape(string shape, string destination)
    {
        string source = Directory.GetFiles("Library/PackageCache", shape + ".png", SearchOption.AllDirectories)
            .First(p => p.Contains("com.unity.2d.sprite@") && p.Contains("/Textures/v2/"));
        File.Copy(source, destination, true);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(destination);
    }

    private static GameObject MakeSprite(string name, Sprite sprite, Color color, Vector2 position, Vector3 scale)
    {
        var obj = new GameObject(name, typeof(SpriteRenderer));
        obj.GetComponent<SpriteRenderer>().sprite = sprite;
        obj.GetComponent<SpriteRenderer>().color = color;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        return obj;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string value, float size, Vector2 dimensions, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = dimensions;
        return text;
    }

    private static void ConfigureTags()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var list = tags.FindProperty("tags");
        foreach (string tag in new[] { "Coin", "Hazard", "Goal" })
        {
            if (Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).stringValue == tag)) continue;
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).stringValue = tag;
        }
        var layers = tags.FindProperty("layers");
        if (LayerMask.NameToLayer("Ground") < 0)
        {
            int index = Enumerable.Range(6, layers.arraySize - 6)
                .First(i => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
            layers.GetArrayElementAtIndex(index).stringValue = "Ground";
        }
        tags.ApplyModifiedPropertiesWithoutUndo();
    }
}
