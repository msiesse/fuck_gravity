using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DoorData : ObstacleTypeData
{
    // Vertical centre of the opening; 0 is the middle of the corridor.
    public float gapCenter;
    public float gapHeight = 4f;
    public float width = 1f;
}

// A wall across the whole corridor with a gap to fly through.
public class DoorObstacle : LevelObstacle<DoorData>
{
    [SerializeField] Transform bottom;
    [SerializeField] Transform top;

    protected override void Apply(DoorData data)
    {
        Place(bottom, Bottom(data));
        Place(top, Top(data));
    }

    protected override IEnumerable<Rect> Parts(DoorData data)
    {
        yield return Bottom(data);
        yield return Top(data);
    }

    static Rect Bottom(DoorData data) => Span(data.width, -Game.PlatformSurfaceY, data.gapCenter - data.gapHeight / 2f);
    static Rect Top(DoorData data) => Span(data.width, data.gapCenter + data.gapHeight / 2f, Game.PlatformSurfaceY);
}
