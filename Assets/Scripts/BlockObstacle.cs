using System;
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

    protected override void Apply(BlockData data)
    {
        float side = data.side == PlatformSide.Floor ? -1f : 1f;
        body.localPosition = new Vector3(0f, side * (Game.PlatformSurfaceY - data.height / 2f), 0f);
        body.localScale = new Vector3(data.width, data.height, 1f);
    }
}
