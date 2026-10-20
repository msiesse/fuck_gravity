using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MovingBlockData : ObstacleTypeData
{
    public float height = 2f;
    public float width = 1f;
    // Lowest and highest points the block's edges reach; the platforms are the limit.
    public float bottom = -Game.PlatformSurfaceY;
    public float top = Game.PlatformSurfaceY;
    // Distance the level scrolls while the block goes from the bottom to the top and back.
    public float cycleDistance = 10f;
    // Where in that cycle the block is when it reaches the player: 0 = bottom, 0.5 = top.
    [Range(0f, 1f)] public float phaseAtPlayer;
}

// A block that moves up and down between two heights. Its position depends only on how far
// the level has scrolled, not on time, so it is identical on every attempt.
public class MovingBlockObstacle : LevelObstacle<MovingBlockData>
{
    [SerializeField] Transform body;

    MovingBlockData data;

    protected override void Apply(MovingBlockData data)
    {
        this.data = data;
        body.localScale = new Vector3(data.width, data.height, 1f);
        Move();
    }

    protected override IEnumerable<Rect> Parts(MovingBlockData data)
    {
        float centre = CentreAt(data, data.phaseAtPlayer);
        yield return Span(data.width, centre - data.height / 2f, centre + data.height / 2f);
    }

    protected override IEnumerable<Rect> Reach(MovingBlockData data)
    {
        yield return Span(data.width, CentreAt(data, 0f) - data.height / 2f, CentreAt(data, 0.5f) + data.height / 2f);
    }

    // After Game.Update has scrolled the level.
    void LateUpdate()
    {
        Move();
    }

    void Move()
    {
        float cycle = data.phaseAtPlayer;
        if (data.cycleDistance > 0f)
            cycle += (Game.PlayerX - transform.position.x) / data.cycleDistance;
        body.localPosition = new Vector3(0f, CentreAt(data, cycle), 0f);
    }

    // Height of the block's centre at a point of its cycle: 0 = bottom, 0.5 = top.
    static float CentreAt(MovingBlockData data, float cycle)
    {
        // Range of the centre; a range too small for the block leaves it still in the middle.
        float lowest = Mathf.Max(data.bottom, -Game.PlatformSurfaceY) + data.height / 2f;
        float highest = Mathf.Min(data.top, Game.PlatformSurfaceY) - data.height / 2f;
        if (highest < lowest)
            lowest = highest = (lowest + highest) / 2f;

        return Mathf.Lerp(lowest, highest, Mathf.PingPong(cycle * 2f, 1f));
    }
}
