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

    [Header("Extras (post-proceso del laberinto)")]
    [Tooltip("Porcentaje de MUROS aleatorios que se añaden sobre pasillos (0-0.25 recomendado)")]
    [Range(0f, 0.5f)] public float extraWalls = 0.10f;
    [Tooltip("Porcentaje de PASAJES extra: derriba paredes entre dos celdas para crear bucles (0-0.30 recomendado)")]
    [Range(0f, 0.6f)] public float extraPassages = 0.15f;

    [Header("Tamaño y espaciado por celda")]
    [Tooltip("Proporción del tamaño de la celda que ocupa el bloque en X (0-1). 1 = llena la celda.")]
    [Range(0.1f, 1f)] public float fillX = 0.85f;
    [Tooltip("Proporción del tamaño de la celda que ocupa el bloque en Y (0-1). 1 = llena la celda.")]
    [Range(0.1f, 1f)] public float fillY = 0.85f;
    [Tooltip("Resta (en unidades mundo) al tamaño final del bloque dentro de la celda. (X=ancho, Y=alto)")]
    public Vector2 extraPadding = Vector2.zero;

    [Header("Paso entre celdas (opcional)")]
    [Tooltip("Si está activo, no se ajusta al área; usa estos pasos fijos entre celdas.")]
    public bool overrideCellStep = false;
    public float cellStepX = 1f;
    public float cellStepY = 1f;

    [Header("Objetos a proteger y conectar")]
    public Transform player;
    public Transform enemy;
    public Transform goal;
    [Tooltip("Radio (en celdas) del área despejada alrededor de Player/Enemy/Goal")]
    [Range(0, 4)] public int safeRadius = 1;

    // --- Internos ---
    // grid[y, x] : 1 = muro, 0 = pasillo
    int[,] grid;
    float stepX, stepY;
    Vector2 origin;
    Vector2 cachedPrefabSize; // tamaño del prefab (bounds) a escala 1
    bool prefabSizeCached = false;

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
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(parentForWalls.GetChild(i).gameObject);
                else Destroy(parentForWalls.GetChild(i).gameObject);
#else
                Destroy(parentForWalls.GetChild(i).gameObject);
#endif
            }
        }

        // grid con impares
        columns = Mathf.Max(5, columns | 1); // fuerza impar
        rows    = Mathf.Max(5, rows    | 1);

        // calcular pasos y origen
        Vector2 bl = areaBottomLeft.position;
        Vector2 tr = areaTopRight.position;

        if (!overrideCellStep)
        {
            float w = Mathf.Abs(tr.x - bl.x);
            float h = Mathf.Abs(tr.y - bl.y);
            stepX = w / (columns - 1);
            stepY = h / (rows - 1);
        }
        else
        {
            stepX = Mathf.Max(0.01f, cellStepX);
            stepY = Mathf.Max(0.01f, cellStepY);
        }

        origin = new Vector2(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y));

        // inicializar todo como muro
        grid = new int[rows, columns];
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                grid[y, x] = 1;

        // carve perfecto con backtracker
        CarveMaze();

        // reforzar inicio/fin (esquinas básicas abiertas)
        grid[1, 1] = 0;
        grid[rows - 2, columns - 2] = 0;

        // post-proceso: añadir muros (dificulta) y añadir pasajes (ramifica)
        AddExtraWalls();      // añade obstáculos sueltos
        AddExtraPassages();   // derriba paredes entre celdas para crear bucles

        // --- Proteger Player/Enemy/Goal y asegurar camino Player->Goal ---
        Vector2Int? cPlayer = WorldToCellSafe(player);
        Vector2Int? cEnemy  = WorldToCellSafe(enemy);
        Vector2Int? cGoal   = WorldToCellSafe(goal);

        if (cPlayer.HasValue) OpenAround(cPlayer.Value, safeRadius);
        if (cEnemy.HasValue)  OpenAround(cEnemy.Value, safeRadius);
        if (cGoal.HasValue)   OpenAround(cGoal.Value, safeRadius);

        if (cPlayer.HasValue && cGoal.HasValue)
        {
            EnsurePath(cPlayer.Value, cGoal.Value);
            OpenAround(cPlayer.Value, Mathf.Max(1, safeRadius));
            OpenAround(cGoal.Value, Mathf.Max(1, safeRadius));
        }

        // instanciar
        CachePrefabSizeIfNeeded();
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

    // NUEVO: crea bucles/ramas derribando paredes que separan dos celdas ya transitables
    void AddExtraPassages()
    {
        // recolecta paredes entre celdas (coordenadas "entre medias") que al derribarse conectan dos pasillos
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int y = 1; y < rows - 1; y++)
        {
            for (int x = 1; x < columns - 1; x++)
            {
                if (grid[y, x] != 1) continue; // solo paredes

                bool isBetweenCells =
                    // pared vertical entre dos celdas horizontales: vecinos izquierda/derecha
                    ((x % 2 == 0) && (y % 2 == 1) && grid[y, x - 1] == 0 && grid[y, x + 1] == 0)
                    ||
                    // pared horizontal entre dos celdas verticales: vecinos arriba/abajo
                    ((x % 2 == 1) && (y % 2 == 0) && grid[y - 1, x] == 0 && grid[y + 1, x] == 0);

                if (isBetweenCells) candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0) return;

        int toOpen = Mathf.RoundToInt(candidates.Count * Mathf.Clamp01(extraPassages));
        for (int i = 0; i < toOpen; i++)
        {
            var pick = candidates[Random.Range(0, candidates.Count)];
            candidates.Remove(pick);
            grid[pick.y, pick.x] = 0; // abrir
        }
    }

    // ---------- RESERVAS Y CAMINO ----------
    Vector2Int? WorldToCellSafe(Transform t)
    {
        if (t == null) return null;

        float fx = (t.position.x - origin.x) / stepX;
        float fy = (t.position.y - origin.y) / stepY;

        int cx = Mathf.RoundToInt(fx);
        int cy = Mathf.RoundToInt(fy);

        if (cx < 0 || cx >= columns || cy < 0 || cy >= rows)
            return null;

        return new Vector2Int(cx, cy);
    }

    void OpenAround(Vector2Int c, int radius)
    {
        int r = Mathf.Max(0, radius);
        for (int y = c.y - r; y <= c.y + r; y++)
        {
            for (int x = c.x - r; x <= c.x + r; x++)
            {
                if (x > 0 && x < columns - 1 && y > 0 && y < rows - 1)
                    grid[y, x] = 0;
            }
        }
    }

    void EnsurePath(Vector2Int start, Vector2Int target)
    {
        var path = AStar(start, target, allowCarve:true);
        if (path == null || path.Count == 0) return;

        foreach (var p in path)
        {
            if (p.x > 0 && p.x < columns - 1 && p.y > 0 && p.y < rows - 1)
                grid[p.y, p.x] = 0;
        }
    }

    List<Vector2Int> AStar(Vector2Int start, Vector2Int goal, bool allowCarve)
    {
        int Cost(Vector2Int p)
        {
            return grid[p.y, p.x] == 0 ? 1 : (allowCarve ? 5 : 9999);
        }

        int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan
        }

        Vector2Int[] neigh = new[]
        {
            new Vector2Int( 1, 0), new Vector2Int(-1, 0),
            new Vector2Int( 0, 1), new Vector2Int( 0,-1),
        };

        var open = new PriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();
        var fScore = new Dictionary<Vector2Int, int>();

        bool InBounds(Vector2Int p) => p.x >= 1 && p.x < columns - 1 && p.y >= 1 && p.y < rows - 1;

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        open.Enqueue(start, fScore[start]);

        while (open.Count > 0)
        {
            var current = open.Dequeue();

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            foreach (var d in neigh)
            {
                var nb = new Vector2Int(current.x + d.x, current.y + d.y);
                if (!InBounds(nb)) continue;

                int tentativeG = gScore[current] + Cost(nb);
                if (!gScore.TryGetValue(nb, out int g) || tentativeG < g)
                {
                    cameFrom[nb] = current;
                    gScore[nb] = tentativeG;
                    int f = tentativeG + Heuristic(nb, goal);
                    fScore[nb] = f;
                    open.EnqueueOrUpdate(nb, f);
                }
            }
        }

        return null;
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> came, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (came.ContainsKey(current))
        {
            current = came[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    // ---------- RENDER ----------
    void RenderWalls()
    {
        float desiredW = Mathf.Max(0.01f, stepX * Mathf.Clamp01(fillX) - Mathf.Max(0f, extraPadding.x));
        float desiredH = Mathf.Max(0.01f, stepY * Mathf.Clamp01(fillY) - Mathf.Max(0f, extraPadding.y));

        Vector3 scaleForPrefab = Vector3.one;
        if (cachedPrefabSize.x > 0.0001f && cachedPrefabSize.y > 0.0001f)
        {
            scaleForPrefab = new Vector3(
                desiredW / cachedPrefabSize.x,
                desiredH / cachedPrefabSize.y,
                1f
            );
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (grid[y, x] == 1)
                {
                    Vector3 pos = CellToWorld(x, y);
                    var go = Instantiate(wallCellPrefab, pos, Quaternion.identity, parentForWalls);

                    // IMPORTANTE: para resultados consistentes, el padre debería tener escala (1,1,1).
                    go.transform.localScale = scaleForPrefab;

                    int layer = LayerMask.NameToLayer("Walls");
                    if (layer >= 0) go.layer = layer;
                }
            }
        }
    }

    Vector3 CellToWorld(int cx, int cy)
    {
        return new Vector3(origin.x + cx * stepX, origin.y + cy * stepY, 0f);
    }

    void CachePrefabSizeIfNeeded()
    {
        if (prefabSizeCached || wallCellPrefab == null) return;

        GameObject temp = Instantiate(wallCellPrefab);
        temp.transform.position = new Vector3(999999, 999999, 0);
        temp.transform.localScale = Vector3.one;
        temp.SetActive(true);

        Renderer r = temp.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            cachedPrefabSize = new Vector2(r.bounds.size.x, r.bounds.size.y);
        }
        else
        {
            cachedPrefabSize = Vector2.one;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(temp);
        else Destroy(temp);
#else
        Destroy(temp);
#endif

        prefabSizeCached = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        fillX = Mathf.Clamp01(fillX);
        fillY = Mathf.Clamp01(fillY);
    }

    void OnDrawGizmosSelected()
    {
        if (areaBottomLeft == null || areaTopRight == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((areaBottomLeft.position + areaTopRight.position) * 0.5f,
                            new Vector3(Mathf.Abs(areaTopRight.position.x - areaBottomLeft.position.x),
                                        Mathf.Abs(areaTopRight.position.y - areaBottomLeft.position.y), 0.1f));
    }
#endif

    // ---------- Priority Queue mínima para A* ----------
    class PriorityQueue<T>
    {
        private readonly List<(T item, int priority)> data = new List<(T, int)>();
        private readonly Dictionary<T, int> indexMap = new Dictionary<T, int>();

        public int Count => data.Count;

        public void Enqueue(T item, int priority)
        {
            data.Add((item, priority));
            indexMap[item] = data.Count - 1;
            HeapifyUp(data.Count - 1);
        }

        public void EnqueueOrUpdate(T item, int priority)
        {
            if (indexMap.TryGetValue(item, out int idx))
            {
                if (priority < data[idx].priority)
                {
                    data[idx] = (item, priority);
                    HeapifyUp(idx);
                }
            }
            else
            {
                Enqueue(item, priority);
            }
        }

        public T Dequeue()
        {
            var root = data[0].item;
            Swap(0, data.Count - 1);
            indexMap.Remove(root);
            data.RemoveAt(data.Count - 1);
            if (data.Count > 0) HeapifyDown(0);
            return root;
        }

        void HeapifyUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (data[i].priority >= data[p].priority) break;
                Swap(i, p);
                i = p;
            }
        }

        void HeapifyDown(int i)
        {
            while (true)
            {
                int l = 2 * i + 1;
                int r = 2 * i + 2;
                int smallest = i;
                if (l < data.Count && data[l].priority < data[smallest].priority) smallest = l;
                if (r < data.Count && data[r].priority < data[smallest].priority) smallest = r;
                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        void Swap(int a, int b)
        {
            var ta = data[a]; var tb = data[b];
            data[a] = tb; data[b] = ta;
            indexMap[data[a].item] = a;
            indexMap[data[b].item] = b;
        }
    }
}
