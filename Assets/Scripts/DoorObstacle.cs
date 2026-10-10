using System;
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
        float gapBottom = data.gapCenter - data.gapHeight / 2f;
        float gapTop = data.gapCenter + data.gapHeight / 2f;
        Fill(bottom, -Game.PlatformSurfaceY, gapBottom, data.width);
        Fill(top, gapTop, Game.PlatformSurfaceY, data.width);
    }

    // Stretches a part so it spans from one height to another; hides it if there is nothing to span.
    static void Fill(Transform part, float from, float to, float width)
    {
        float height = to - from;
        part.gameObject.SetActive(height > 0f);
        part.localPosition = new Vector3(0f, (from + to) / 2f, 0f);
        part.localScale = new Vector3(width, Mathf.Max(height, 0f), 1f);
    }
}
