using UnityEngine;

public class FreeMovementRule : IMovementRule
{
    readonly TableGrid grid;

    public FreeMovementRule(TableGrid grid)
    {
        this.grid = grid;
    }

    public Vector3 Snap(Vector3 worldPoint, int size)
    {
        float half = size * grid.CellSize / 2f;
        Vector2 min = grid.Min;
        Vector2 max = grid.Max;
        
        return new Vector3(
            Mathf.Clamp(worldPoint.x, min.x + half, max.x - half),
            0f,
            Mathf.Clamp(worldPoint.z, min.y + half, max.y - half)
        );
    }

    public float MeasureDistance(Vector3 from, Vector3 to)
    {
        from.y = 0f;
        to.y = 0f;
        return Vector3.Distance(from, to);
    }
}
