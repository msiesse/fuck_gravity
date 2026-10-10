using UnityEngine;

// A single block attached to the floor or the ceiling. Uses side, height and width.
public class BlockObstacle : LevelObstacle
{
    [SerializeField] Transform body;

    public override string[] DataFields => new[]
    {
        nameof(ObstacleData.side), nameof(ObstacleData.height), nameof(ObstacleData.width)
    };

    public override void Configure(ObstacleData data)
    {
        float side = data.side == PlatformSide.Floor ? -1f : 1f;
        body.localPosition = new Vector3(0f, side * (Game.PlatformSurfaceY - data.height / 2f), 0f);
        body.localScale = new Vector3(data.width, data.height, 1f);
    }
}
