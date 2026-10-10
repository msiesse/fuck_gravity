using UnityEngine;

// Base class of the script on the root of every obstacle prefab. One prefab = one obstacle type.
public abstract class LevelObstacle : MonoBehaviour
{
    // Names of the ObstacleData fields this type reads. The level Inspector hides the others.
    public abstract string[] DataFields { get; }

    // Sizes and places the prefab's parts from the level data. The root itself is already
    // positioned at the obstacle's distance, centred vertically in the corridor.
    public abstract void Configure(ObstacleData data);
}
