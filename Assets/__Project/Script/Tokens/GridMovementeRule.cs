using UnityEngine;

// Modo Grade: o token sempre para alinhado aos quadrados.
// Distância com a regra "diagonal = 1 quadrado" (padrão do D&D 2024).
public class GridMovementRule : IMovementRule
{
    readonly TableGrid grid;

    public GridMovementRule(TableGrid grid)
    {
        this.grid = grid;
    }

    public Vector3 Snap(Vector3 position, int sizeInCells)
    {
        float cell = grid.CellSize;
        Vector2 min = grid.Min;
        Vector2 max = grid.Max;
        float half = sizeInCells * cell / 2f;

        // Trabalha com o canto inferior esquerdo do token: ele precisa cair numa linha da grade.
        // Isso funciona para tokens 1x1 (centro no meio do quadrado) e 2x2 (centro num cruzamento de linhas).
        int cornerX = Mathf.RoundToInt((position.x - half - min.x) / cell);
        int cornerZ = Mathf.RoundToInt((position.z - half - min.y) / cell);

        // Mantém o token inteiro dentro da grade.
        int cellsX = Mathf.RoundToInt((max.x - min.x) / cell);
        int cellsZ = Mathf.RoundToInt((max.y - min.y) / cell);
        cornerX = Mathf.Clamp(cornerX, 0, Mathf.Max(0, cellsX - sizeInCells));
        cornerZ = Mathf.Clamp(cornerZ, 0, Mathf.Max(0, cellsZ - sizeInCells));

        return new Vector3(min.x + cornerX * cell + half, position.y, min.y + cornerZ * cell + half);
    }

    public float MeasureDistance(Vector3 from, Vector3 to)
    {
        float cell = grid.CellSize;
        int dx = Mathf.Abs(Mathf.RoundToInt((to.x - from.x) / cell));
        int dz = Mathf.Abs(Mathf.RoundToInt((to.z - from.z) / cell));
        // Diagonal = 1 quadrado: o custo é o maior dos dois deslocamentos.
        return Mathf.Max(dx, dz) * cell;
    }
}
