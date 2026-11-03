using UnityEngine;

public interface IMovementRule
{
    Vector3 Snap(Vector3 position, int sizeInCells);

    float MeasureDistance(Vector3 from, Vector3 to);

}
