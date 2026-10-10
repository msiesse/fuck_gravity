using System.Collections.Generic;
using UnityEngine;

// Builds the whole prototype in code when Play is pressed, so the scene needs no setup.
public class Game : MonoBehaviour
{
    const float PlatformSurfaceY = 4f;   // inner face of each platform (corridor is 2x this tall)
    const float PlatformThickness = 1f;
    const float PlatformWidth = 60f;
    const float PlayerX = -5f;
    const float SpawnX = 14f;
    const float DespawnX = -14f;

    public float scrollSpeed = 8f;
    public float spawnInterval = 1.2f;
    public float minObstacleHeight = 1f;
    public float maxObstacleHeight = 3f;

    readonly List<Transform> obstacles = new List<Transform>();
    Player player;
    float spawnTimer;
    float score;
    float best;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        new GameObject("Game").AddComponent<Game>();
    }

    void Start()
    {
        SetUpCamera();
        CreatePlatform("Floor", -1f);
        CreatePlatform("Ceiling", 1f);
        CreatePlayer();
        Restart();
    }

    void Update()
    {
        score += Time.deltaTime;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnObstacle();
        }

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            Transform obstacle = obstacles[i];
            obstacle.position += Vector3.left * (scrollSpeed * Time.deltaTime);
            if (obstacle.position.x < DespawnX)
            {
                Destroy(obstacle.gameObject);
                obstacles.RemoveAt(i);
            }
        }
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28 };
        GUI.Label(new Rect(20, 16, 600, 40), $"{score:0.0}s   best {best:0.0}s", style);
    }

    void Restart()
    {
        best = Mathf.Max(best, score);
        score = 0f;
        spawnTimer = spawnInterval;

        foreach (Transform obstacle in obstacles)
            Destroy(obstacle.gameObject);
        obstacles.Clear();

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

    void SpawnObstacle()
    {
        float height = Random.Range(minObstacleHeight, maxObstacleHeight);
        float side = Random.value < 0.5f ? -1f : 1f;

        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Obstacle";
        obstacle.transform.position = new Vector3(SpawnX, side * (PlatformSurfaceY - height / 2f), 0f);
        obstacle.transform.localScale = new Vector3(1f, height, 1f);
        obstacle.GetComponent<Renderer>().material.color = new Color(1f, 0.3f, 0.3f);
        obstacle.GetComponent<Collider>().isTrigger = true;
        obstacle.AddComponent<Rigidbody>().isKinematic = true;
        obstacle.AddComponent<Obstacle>();
        obstacles.Add(obstacle.transform);
    }
}
