using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlatformSide
{
    Floor,
    Ceiling
}

[Serializable]
public class BlockData : ObstacleTypeData
{
    public PlatformSide side;
    public float height = 2f;
    public float width = 1f;
}

// A single block attached to the floor or the ceiling.
public class BlockObstacle : LevelObstacle<BlockData>
{
    [SerializeField] Transform body;

    protected override void Apply(BlockData data) => Place(body, Body(data));

    protected override IEnumerable<Rect> Parts(BlockData data)
    {
        yield return Body(data);
    }

    static Rect Body(BlockData data)
    {
        return data.side == PlatformSide.Floor
            ? Span(data.width, -Game.PlatformSurfaceY, -Game.PlatformSurfaceY + data.height)
            : Span(data.width, Game.PlatformSurfaceY - data.height, Game.PlatformSurfaceY);
    }
}
