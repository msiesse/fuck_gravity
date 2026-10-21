using System.Collections.Generic;
using UnityEngine;

// Builds the whole prototype in code when Play is pressed, so the scene needs no setup.
public class Game : MonoBehaviour
{
    public const string CatalogPath = "LevelCatalog";   // relative to a Resources folder
    public const float PlatformSurfaceY = 4f;   // inner face of each platform (corridor is 2x this tall)
    public const float PlatformThickness = 1f;
    const float PlatformWidth = 60f;
    public const float PlayerX = -5f;
    const float CompleteDelay = 2f;
    public const float CollectibleSize = 0.6f;

#if UNITY_EDITOR
    // Set by the level editor's Play buttons for the next Play only: the asset path of the level
    // to start with, and the distance to start its attempts from.
    public const string TestLevelKey = "FuckGravity.TestLevel";
    public const string TestStartKey = "FuckGravity.TestStart";
#endif

    LevelCatalog catalog;
    // Where attempts start; only the level editor sets it, for the first level played.
    float startDistance;
    int levelIndex;
    LevelData level;
    Transform levelRoot;
    Player player;
    float travelled;
    float bestProgress;
    float completeTimer;
    int attempts;
    readonly List<Collectible> collectibles = new List<Collectible>();
    int collected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        new GameObject("Game").AddComponent<Game>();
    }

    void Start()
    {
        catalog = Resources.Load<LevelCatalog>(CatalogPath);
        int firstLevel = 0;
#if UNITY_EDITOR
        firstLevel = LoadTestLevel();
#endif
        if (catalog == null || catalog.levels.Count == 0)
        {
            Debug.LogError($"No LevelCatalog with levels found at Resources/{CatalogPath}");
            enabled = false;
            return;
        }

        SetUpCamera();
        CreatePlatform("Floor", -1f);
        CreatePlatform("Ceiling", 1f);
        CreatePlayer();
        LoadLevel(firstLevel);
    }

#if UNITY_EDITOR
    // Returns the index of the level the level editor asked to start with, if any. The game then
    // carries on through the catalog; a level not in the catalog is played on its own, on repeat.
    int LoadTestLevel()
    {
        string path = UnityEditor.SessionState.GetString(TestLevelKey, "");
        startDistance = UnityEditor.SessionState.GetFloat(TestStartKey, 0f);
        UnityEditor.SessionState.EraseString(TestLevelKey);
        UnityEditor.SessionState.EraseFloat(TestStartKey);

        var testLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (testLevel == null)
        {
            startDistance = 0f;
            return 0;
        }

        startDistance = Mathf.Clamp(startDistance, 0f, testLevel.length);
        int index = catalog != null ? catalog.levels.IndexOf(testLevel) : -1;
        if (index >= 0)
            return index;

        catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        catalog.levels.Add(testLevel);
        return 0;
    }
#endif

    void Update()
    {
        if (travelled >= level.length)
        {
            completeTimer -= Time.deltaTime;
            if (completeTimer <= 0f)
            {
                startDistance = 0f;
                LoadLevel((levelIndex + 1) % catalog.levels.Count);
            }
            return;
        }

        travelled = Mathf.Min(travelled + level.scrollSpeed * Time.deltaTime, level.length);
        bestProgress = Mathf.Max(bestProgress, Progress);
        levelRoot.position = Vector3.left * travelled;
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28 };
        string text = travelled >= level.length
            ? $"{level.name} complete!"
            : $"{level.name}   {Progress:0}%   best {bestProgress:0}%   attempt {attempts}   coins {collected}/{collectibles.Count}";
        GUI.Label(new Rect(20, 16, 800, 40), text, style);
    }

    float Progress => 100f * travelled / level.length;

    // Swaps the obstacles and collectibles for those of another level; platforms, camera and player are kept.
    void LoadLevel(int index)
    {
        if (levelRoot != null)
            Destroy(levelRoot.gameObject);

        levelIndex = index;
        level = catalog.levels[index];
        bestProgress = 0f;
        attempts = 0;
        BuildLevel();
        Restart();
    }

    void Restart()
    {
        attempts++;
        travelled = startDistance;
        completeTimer = CompleteDelay;
        collected = 0;
        foreach (Collectible collectible in collectibles)
            collectible.gameObject.SetActive(true);
        levelRoot.position = Vector3.left * travelled;
        player.ResetTo(new Vector3(PlayerX, -PlatformSurfaceY + 0.5f, 0f));
    }

    void SetUpCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = PlatformSurfaceY + PlatformThickness;
        cam.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
    }

    void CreatePlatform(string platformName, float side)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = platformName;
        platform.transform.position = new Vector3(0f, side * (PlatformSurfaceY + PlatformThickness / 2f), 0f);
        platform.transform.localScale = new Vector3(PlatformWidth, PlatformThickness, 1f);
        platform.GetComponent<Renderer>().material.color = new Color(0.35f, 0.38f, 0.45f);
    }

    void CreatePlayer()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Player";
        cube.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 1f);
        player = cube.AddComponent<Player>();
        player.Died += Restart;
        player.Collected += Collect;
    }

    void Collect(Collectible collectible)
    {
        collectible.gameObject.SetActive(false);
        collected++;
    }

    // Every obstacle and collectible is created once, as a child of one root that scrolls left as a whole.
    void BuildLevel()
    {
        levelRoot = new GameObject("Level").transform;
        levelRoot.gameObject.AddComponent<Rigidbody>().isKinematic = true;

        foreach (ObstacleData data in level.obstacles)
        {
            if (data.prefab == null || data.typeData == null)
            {
                Debug.LogWarning($"{level.name}: obstacle at distance {data.distance} has no prefab or data, skipped");
                continue;
            }

            LevelObstacle obstacle = Instantiate(data.prefab, levelRoot);
            obstacle.transform.localPosition = new Vector3(PlayerX + data.distance, 0f, 0f);
            obstacle.Configure(data.typeData);
        }

        collectibles.Clear();
        foreach (CollectibleData data in level.collectibles)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coin.name = "Collectible";
            coin.transform.SetParent(levelRoot);
            coin.transform.localPosition = new Vector3(PlayerX + data.distance, data.y, 0f);
            coin.transform.localScale = Vector3.one * CollectibleSize;
            coin.GetComponent<Renderer>().material.color = new Color(1f, 0.8f, 0.1f);
            coin.GetComponent<Collider>().isTrigger = true;
            collectibles.Add(coin.AddComponent<Collectible>());
        }
    }
}
