using System;
using UnityEngine;

// Base class of the script on the root of every obstacle prefab. One prefab = one obstacle type.
// Obstacle types derive from LevelObstacle<TData> below, not from this class directly.
public abstract class LevelObstacle : MonoBehaviour
{
    public abstract Type DataType { get; }
    public abstract ObstacleTypeData CreateData();

    // Sizes and places the prefab's parts from the level data. The root itself is already
    // positioned at the obstacle's distance, centred vertically in the corridor.
    public abstract void Configure(ObstacleTypeData data);
}

// Ties an obstacle type to its own data class, so each type only sees its own parameters.
public abstract class LevelObstacle<TData> : LevelObstacle where TData : ObstacleTypeData, new()
{
    public sealed override Type DataType => typeof(TData);
    public sealed override ObstacleTypeData CreateData() => new TData();
    public sealed override void Configure(ObstacleTypeData data) => Apply((TData)data);

    protected abstract void Apply(TData data);
}
