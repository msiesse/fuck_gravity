using System;
using System.Collections.Generic;
using UnityEngine;

// Base class of the per-type parameters of an obstacle (BlockData, DoorData, ...).
[Serializable]
public abstract class ObstacleTypeData
{
    public ObstacleTypeData Clone() => (ObstacleTypeData)MemberwiseClone();
}

// One obstacle in a level: what every obstacle has, plus the parameters of its own type.
[Serializable]
public class ObstacleData
{
    // How far the level has to scroll before this obstacle reaches the player.
    public float distance;
    // The obstacle type: a prefab from Assets/Prefabs/Obstacles.
    public LevelObstacle prefab;
    // Parameters of that type. Its class is decided by the prefab, see LevelData.OnValidate.
    [SerializeReference] public ObstacleTypeData typeData;
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

    // Runs in the editor whenever the level is loaded or edited: gives every obstacle the data
    // class its prefab asks for, and its own instance of it (adding a list entry in the Inspector
    // can leave two entries pointing at the same data).
    void OnValidate()
    {
        var seen = new HashSet<ObstacleTypeData>();
        foreach (ObstacleData obstacle in obstacles)
        {
            if (obstacle.prefab == null)
                obstacle.typeData = null;
            else if (obstacle.typeData == null || obstacle.typeData.GetType() != obstacle.prefab.DataType)
                obstacle.typeData = obstacle.prefab.CreateData();
            else if (!seen.Add(obstacle.typeData))
                obstacle.typeData = obstacle.typeData.Clone();
        }
    }
}
