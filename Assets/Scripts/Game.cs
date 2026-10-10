using UnityEngine;

// Builds the whole prototype in code when Play is pressed, so the scene needs no setup.
public class Game : MonoBehaviour
{
    const string LevelPath = "Levels/Level1";   // relative to a Resources folder
    const float PlatformSurfaceY = 4f;   // inner face of each platform (corridor is 2x this tall)
    const float PlatformThickness = 1f;
    const float PlatformWidth = 60f;
    const float PlayerX = -5f;
    const float CompleteDelay = 2f;

    LevelData level;
    Transform levelRoot;
    Player player;
    float travelled;
    float bestProgress;
    float completeTimer;
    int attempts;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        new GameObject("Game").AddComponent<Game>();
    }

    void Start()
    {
        level = Resources.Load<LevelData>(LevelPath);
        if (level == null)
        {
            Debug.LogError($"No LevelData found at Resources/{LevelPath}");
            enabled = false;
            return;
        }

        SetUpCamera();
        CreatePlatform("Floor", -1f);
        CreatePlatform("Ceiling", 1f);
        BuildLevel();
        CreatePlayer();
        Restart();
    }

    void Update()
    {
        if (travelled >= level.length)
        {
            completeTimer -= Time.deltaTime;
            if (completeTimer <= 0f)
                Restart();
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
            ? "Level complete!"
            : $"{Progress:0}%   best {bestProgress:0}%   attempt {attempts}";
        GUI.Label(new Rect(20, 16, 800, 40), text, style);
    }

    float Progress => 100f * travelled / level.length;

    void Restart()
    {
        attempts++;
        travelled = 0f;
        completeTimer = CompleteDelay;
        levelRoot.position = Vector3.zero;
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
    }

    // Every obstacle is created once, as a child of one root that scrolls left as a whole.
    void BuildLevel()
    {
        levelRoot = new GameObject("Level").transform;
        levelRoot.gameObject.AddComponent<Rigidbody>().isKinematic = true;

        foreach (ObstacleData data in level.obstacles)
        {
            float side = data.side == PlatformSide.Floor ? -1f : 1f;

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Obstacle";
            obstacle.transform.SetParent(levelRoot);
            obstacle.transform.localPosition = new Vector3(PlayerX + data.distance, side * (PlatformSurfaceY - data.height / 2f), 0f);
            obstacle.transform.localScale = new Vector3(data.width, data.height, 1f);
            obstacle.GetComponent<Renderer>().material.color = new Color(1f, 0.3f, 0.3f);
            obstacle.GetComponent<Collider>().isTrigger = true;
            obstacle.AddComponent<Obstacle>();
        }
    }
}
