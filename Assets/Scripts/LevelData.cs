using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlatformSide
{
    Floor,
    Ceiling
}

[Serializable]
public class ObstacleData
{
    // How far the level has to scroll before this obstacle reaches the player.
    public float distance;
    // The obstacle type: a prefab from Assets/Prefabs/Obstacles. Each type reads only the fields
    // it lists in LevelObstacle.DataFields, and the Inspector shows only those.
    public LevelObstacle prefab;
    public float width = 1f;
    public PlatformSide side;
    public float height = 2f;
    // Vertical centre of a door's opening; 0 is the middle of the corridor.
    public float gapCenter;
    public float gapHeight = 4f;
}

// A hand-authored level. Everything is expressed in world units of distance, not time,
// so changing scrollSpeed makes the level faster without moving the obstacles.
[CreateAssetMenu(fileName = "Level", menuName = "Fuck Gravity/Level")]
public class LevelData : ScriptableObject
{
    public float scrollSpeed = 8f;
    // Distance at which the level is complete.
    public float length = 100f;
    public List<ObstacleData> obstacles = new List<ObstacleData>();
}
