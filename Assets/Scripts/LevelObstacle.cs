using System;
using System.Collections.Generic;
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

    // The parts' rectangles, in the root's local space, at the moment the obstacle reaches the player.
    // The level editor draws these; it calls them on the prefab asset, so they must not use instance state.
    public abstract IEnumerable<Rect> PreviewParts(ObstacleTypeData data);
    // The whole area the parts move through, for obstacles that move.
    public abstract IEnumerable<Rect> PreviewReach(ObstacleTypeData data);

    // Stretches a part over a rectangle; hides it if the rectangle is empty.
    protected static void Place(Transform part, Rect rect)
    {
        part.gameObject.SetActive(rect.height > 0f);
        part.localPosition = new Vector3(rect.center.x, rect.center.y, 0f);
        part.localScale = new Vector3(rect.width, Mathf.Max(rect.height, 0f), 1f);
    }
}

// Ties an obstacle type to its own data class, so each type only sees its own parameters.
public abstract class LevelObstacle<TData> : LevelObstacle where TData : ObstacleTypeData, new()
{
    public sealed override Type DataType => typeof(TData);
    public sealed override ObstacleTypeData CreateData() => new TData();
    public sealed override void Configure(ObstacleTypeData data) => Apply((TData)data);
    public sealed override IEnumerable<Rect> PreviewParts(ObstacleTypeData data) => Parts((TData)data);
    public sealed override IEnumerable<Rect> PreviewReach(ObstacleTypeData data) => Reach((TData)data);

    protected abstract void Apply(TData data);
    protected abstract IEnumerable<Rect> Parts(TData data);
    protected virtual IEnumerable<Rect> Reach(TData data) => Array.Empty<Rect>();

    // A rectangle centred on the obstacle horizontally, spanning from one height to another.
    protected static Rect Span(float width, float from, float to) => Rect.MinMaxRect(-width / 2f, from, width / 2f, to);
}
