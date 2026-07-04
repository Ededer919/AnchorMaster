using UnityEngine;

/// <summary>
/// Builds the game scene: ground, grid, wind system, camera, and initial objects.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 5;
    public int cols = 5;
    public float cellSize = 1f;
    public int objectCount = 4;

    [Header("Ground Settings")]
    public float groundWidth = 30f;
    public float groundHeight = 4f;

    [Header("Balloons")]
    public int balloonCount = 1;

    public GridSystem GridSystem { get; private set; }
    public GridObjectManager ObjectManager { get; private set; }
    public WindSystem2D WindSystem { get; private set; }
    public Transform ObjectsParent { get; private set; }
    public float GroundTopY { get; private set; }

    /// <summary>
    /// Grid bounds for wind culling. Wind only applies inside this rect.
    /// </summary>
    public Rect GridBounds { get; private set; }

    /// <summary>
    /// Returns the top-left corner position of the grid (for object staging area).
    /// </summary>
    public Vector3 GetGridTopLeft()
    {
        return new Vector3(
            -cols * cellSize / 2f - cellSize * 2f,
            GroundTopY + rows * cellSize + cellSize * 0.5f,
            0
        );
    }

    private Sprite sharedSprite;
    private int spawnedCount;

    public void Build()
    {
        sharedSprite = CreateWhiteSprite();

        var rootGo = new GameObject("GameWorld");
        var root = rootGo.transform;

        float gridHeight = rows * cellSize;
        GroundTopY = -gridHeight / 2f;
        CreateGround(root);

        Vector2 origin = new Vector2(-cols * cellSize / 2f, GroundTopY + cellSize / 2f);
        var gridGo = new GameObject("GridSystem");
        gridGo.transform.SetParent(root, false);
        GridSystem = gridGo.AddComponent<GridSystem>();
        GridSystem.cellSize = cellSize;
        GridSystem.gridOrigin = origin;

        var cellPrefab = CreateCellPrefab();
        GridSystem.InitializeGrid(rows, cols, cellPrefab, gridGo.transform);
        Destroy(cellPrefab);

        ColorCells();

        // Calculate grid bounds for wind culling
        float gridMinX = -cols * cellSize / 2f - cellSize * 0.5f;
        float gridMaxX = cols * cellSize / 2f + cellSize * 0.5f;
        float gridMinY = GroundTopY - cellSize * 0.5f;
        float gridMaxY = GroundTopY + rows * cellSize + cellSize * 0.5f;
        GridBounds = new Rect(gridMinX, gridMinY, gridMaxX - gridMinX, gridMaxY - gridMinY);

        var mgrGo = new GameObject("GridObjectManager");
        mgrGo.transform.SetParent(root, false);
        ObjectManager = mgrGo.AddComponent<GridObjectManager>();
        ObjectManager.gridSystem = GridSystem;

        var windGo = new GameObject("WindSystem");
        windGo.transform.SetParent(root, false);
        WindSystem = windGo.AddComponent<WindSystem2D>();
        WindSystem.enableGlobalWind = true;
        WindSystem.globalWindDirection = new Vector2(1, 0);
        WindSystem.globalWindStrength = 0f;
        WindSystem.enableNaturalWind = true;
        WindSystem.naturalWindStrength = 0f;

        var objsGo = new GameObject("GridObjects");
        objsGo.transform.SetParent(root, false);
        ObjectsParent = objsGo.transform;
        ObjectManager.SetObjectsParent(ObjectsParent);
        CreateInitialObjects();
        CreateBalloons();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            // Fit grid + staging area above + ground below
            float viewHeight = rows * cellSize + groundHeight + cellSize * 3f;
            float viewWidth = cols * cellSize + cellSize * 4f;
            cam.orthographicSize = Mathf.Max(viewHeight * 0.6f, viewWidth * 0.6f / cam.aspect);
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.14f);
        }

        Debug.Log("[SceneBuilder] Scene built.");
    }

    public GridObject CreateObject(string name, Color color, Vector3 position)
    {
        return CreateObject(ObjectType.Plank, name, color, position);
    }

    public GridObject CreateObject(ObjectType type, string name, Color color, Vector3 position)
    {
        var config = ObjectConfig.Get(type);

        var go = new GameObject(name);
        go.transform.SetParent(ObjectsParent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * cellSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSprite;
        sr.color = color;
        sr.sortingOrder = 10;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one * 0.9f;

        var obj = go.AddComponent<GridObject>();
        obj.objectID = name.ToLower() + "_" + spawnedCount++;
        obj.displayName = name;
        obj.objectType = type;
        obj.mass = config.mass;
        obj.snapSpeed = 15f;
        obj.receiveWind = true;
        obj.windSensitivity = config.windSensitivity;
        obj.windSwaySpeed = 3f;

        ObjectManager.Register(obj);
        return obj;
    }

    private void CreateGround(Transform parent)
    {
        var go = new GameObject("Ground");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0, GroundTopY - groundHeight / 2f, 0);
        go.transform.localScale = new Vector3(groundWidth, groundHeight, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSprite;
        sr.color = new Color(0.18f, 0.22f, 0.16f, 1f);
        sr.sortingOrder = -1;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    private Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(32, 32);
        var px = new Color[32 * 32];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32);
    }

    private GameObject CreateCellPrefab()
    {
        var prefab = new GameObject("CellPrefab");
        var sr = prefab.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSprite;
        sr.sortingOrder = 0;
        prefab.transform.localScale = Vector3.one * cellSize;
        var gc = prefab.AddComponent<GridCell>();
        gc.backgroundRenderer = sr;
        prefab.SetActive(false);
        return prefab;
    }

    private void ColorCells()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cell = GridSystem.GetCell(r, c);
                if (cell != null && cell.backgroundRenderer != null)
                {
                    cell.backgroundRenderer.color = (r + c) % 2 == 0
                        ? new Color(0.25f, 0.25f, 0.32f, 0.8f)
                        : new Color(0.2f, 0.2f, 0.27f, 0.8f);
                }
            }
        }
    }

    private void CreateInitialObjects()
    {
        // No initial objects — all spawned via BuildSystem
    }

    /// <summary>
    /// Randomly places balloons on the grid as protection targets.
    /// </summary>
    private void CreateBalloons()
    {
        for (int i = 0; i < balloonCount; i++)
        {
            // Find a random empty cell
            int attempts = 0;
            int maxAttempts = rows * cols;
            int r, c;

            do
            {
                r = Random.Range(0, rows);
                c = Random.Range(0, cols);
                attempts++;
            }
            while (!GridSystem.CanPlaceAt(r, c) && attempts < maxAttempts);

            if (GridSystem.CanPlaceAt(r, c))
            {
                int colorIdx = i % ObjectConfig.BalloonColors.Length;
                Color color = ObjectConfig.BalloonColors[colorIdx];
                string name = ObjectConfig.BalloonNames[colorIdx];

                Vector2 worldPos = GridSystem.GridToWorldPosition(r, c);
                var obj = CreateObject(ObjectType.Balloon, name, color, worldPos);
                obj.PlaceOnGrid(GridSystem, r, c);

                Debug.Log($"[SceneBuilder] Balloon '{name}' placed at ({r},{c})");
            }
        }

        if (balloonCount > 0)
            UIManager.Instance?.UpdateStatus($"已生成 {balloonCount} 个气球 — 用建材保护它们!");
    }
}
