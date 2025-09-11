using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator2D : MonoBehaviour
{
    [Header("Área interna (dentro de tus paredes)")]
    public Transform areaBottomLeft;
    public Transform areaTopRight;
    public Transform parentForWalls;

    [Header("Grid (usar impares)")]
    public int columns = 21;
    public int rows = 13;

    [Header("Prefab pared interna")]
    public GameObject wallCellPrefab;

    [Header("Semilla")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Extras")]
    [Tooltip("Porcentaje de muros adicionales aleatorios después del laberinto (0-0.25 recomendado)")]
    [Range(0f, 0.5f)] public float extraWalls = 0.10f;

    int[,] grid; // 1 = muro, 0 = pasillo
    float stepX, stepY;
    Vector2 origin;

    void Start()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (useRandomSeed) seed = Random.Range(0, int.MaxValue);
        Random.InitState(seed);

        // limpiar hijos anteriores
        if (parentForWalls != null)
        {
            for (int i = parentForWalls.childCount - 1; i >= 0; i--)
                DestroyImmediate(parentForWalls.GetChild(i).gameObject);
        }

        // grid con impares
        columns = Mathf.Max(5, columns | 1); // fuerza impar
        rows    = Mathf.Max(5, rows | 1);

        // calcular pasos en mundo
        Vector2 bl = areaBottomLeft.position;
        Vector2 tr = areaTopRight.position;
        float w = Mathf.Abs(tr.x - bl.x);
        float h = Mathf.Abs(tr.y - bl.y);

        stepX = w / (columns - 1);
        stepY = h / (rows - 1);
        origin = new Vector2(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y));

        // inicializar todo como muro
        grid = new int[rows, columns];
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                grid[y, x] = 1;

        // carve perfecto con backtracker
        CarveMaze();

        // reforzar inicio/fin (esquinas)
        grid[1, 1] = 0;
        grid[rows - 2, columns - 2] = 0;

        // muros extra aleatorios (sin bloquear demasiado)
        AddExtraWalls();

        // instanciar
        RenderWalls();
    }

    void CarveMaze()
    {
        // celdas "reales" en impares, paredes en pares
        Vector2Int start = new Vector2Int(1, 1);
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        grid[start.y, start.x] = 0;
        stack.Push(start);

        Vector2Int[] dirs = new[] {
            new Vector2Int( 2, 0), new Vector2Int(-2, 0),
            new Vector2Int( 0, 2), new Vector2Int( 0,-2)
        };

        while (stack.Count > 0)
        {
            var cur = stack.Peek();
            // vecinos a 2 pasos que son muro
            List<Vector2Int> neigh = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                int nx = cur.x + d.x;
                int ny = cur.y + d.y;
                if (nx > 0 && nx < columns - 1 && ny > 0 && ny < rows - 1 && grid[ny, nx] == 1)
                    neigh.Add(new Vector2Int(nx, ny));
            }

            if (neigh.Count > 0)
            {
                var n = neigh[Random.Range(0, neigh.Count)];
                // abrir pared entre medias
                int mx = cur.x + (n.x - cur.x) / 2;
                int my = cur.y + (n.y - cur.y) / 2;
                grid[my, mx] = 0;
                grid[n.y, n.x] = 0;
                stack.Push(n);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    void AddExtraWalls()
    {
        int candidates = Mathf.RoundToInt(columns * rows * extraWalls);
        int tries = 0, placed = 0;

        while (placed < candidates && tries < candidates * 10)
        {
            tries++;
            int x = Random.Range(1, columns - 1);
            int y = Random.Range(1, rows - 1);
            if (grid[y, x] == 0)
            {
                // evita bloquear esquinas cercanas al inicio/fin
                if ((x < 3 && y < 3) || (x > columns - 4 && y > rows - 4)) continue;
                grid[y, x] = 1;
                placed++;
            }
        }
    }

    void RenderWalls()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (grid[y, x] == 1)
                {
                    Vector3 pos = CellToWorld(x, y);
                    var go = Instantiate(wallCellPrefab, pos, Quaternion.identity, parentForWalls);
                    go.layer = LayerMask.NameToLayer("Walls");
                }
            }
        }
    }

    Vector3 CellToWorld(int cx, int cy)
    {
        return new Vector3(origin.x + cx * stepX, origin.y + cy * stepY, 0f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (areaBottomLeft == null || areaTopRight == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((areaBottomLeft.position + areaTopRight.position) * 0.5f,
                            new Vector3(Mathf.Abs(areaTopRight.position.x - areaBottomLeft.position.x),
                                        Mathf.Abs(areaTopRight.position.y - areaBottomLeft.position.y), 0.1f));
    }
#endif
}
