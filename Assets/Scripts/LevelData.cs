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
    public PlatformSide side;
    public float height = 2f;
    public float width = 1f;
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
