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

    [Header("Level Background")]
    public int levelIndex = 1;

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
        rootGo.layer = 8; // Game
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

        // Place bedrock on left and right edges (after ObjectsParent is ready)
        CreateBedrock();

        // Place balloons (interior only, not on edges)
        CreateBalloons();

        // Create world-space sky/sun/clouds on Game layer (no colliders → won't block dragging)
        CreateSky(root);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f, 1f); // sky blue
            float viewHeight = rows * cellSize + groundHeight + cellSize * 3f;
            float viewWidth = cols * cellSize + cellSize * 4f;
            cam.orthographicSize = Mathf.Max(viewHeight * 0.6f, viewWidth * 0.6f / cam.aspect);

            // Sync UI Camera orthographicSize so Canvas (ScreenSpaceCamera) aligns with game world
            var uiCam = UnityEngine.Object.FindObjectsByType<Camera>(UnityEngine.FindObjectsSortMode.None);
            foreach (var c in uiCam)
            {
                if (c != cam && c.gameObject.name == "UI Camera")
                {
                    c.orthographicSize = cam.orthographicSize;
                    c.clearFlags = CameraClearFlags.Depth;
                }
            }
        }

        // Set all GameWorld descendants to Game layer
        SetLayerRecursive(root, 8);

        // Add ambient animation (sun rotation, cloud drift)
        rootGo.AddComponent<SceneAnimator>();

        Debug.Log("[SceneBuilder] Scene built.");
    }

    private void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    public GridObject CreateObject(string name, Color color, Vector3 position)
    {
        return CreateObject(ObjectType.Plank, name, color, position);
    }

    public GridObject CreateObject(ObjectType type, string name, Color color, Vector3 position)
    {
        var config = ObjectConfig.Get(type);

        // Try loading prefab from Resources/UI/Prefabs/Objects/
        string prefabPath = "UI/Prefabs/Objects/" + type.ToString();
        var prefab = Resources.Load<GameObject>(prefabPath);

        GameObject go;
        GridObject obj;

        if (prefab != null)
        {
            // Instantiate from prefab
            go = Instantiate(prefab, ObjectsParent);
            go.name = name;
            go.layer = 8; // Game

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Keep prefab's sprite; only set color for Balloon (multi-color)
                if (sr.sprite == null) sr.sprite = sharedSprite;
                if (type == ObjectType.Balloon)
                    sr.color = color; // Balloon uses random color tint
                else
                    sr.color = Color.white; // Other types use texture as-is
                sr.sortingOrder = 10;
            }
        }
        else
        {
            // Fallback: create from code
            go = new GameObject(name);
            go.transform.SetParent(ObjectsParent, false);
            go.layer = 8; // Game

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sharedSprite;
            sr.color = color;
            sr.sortingOrder = 10;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.9f;

            go.AddComponent<GridObject>();
        }

        go.transform.position = position;
        // Scale so the sprite fills exactly one cell, regardless of its natural size
        var objSr = go.GetComponent<SpriteRenderer>();
        if (objSr != null && objSr.sprite != null)
        {
            var b = objSr.sprite.bounds.size;
            if (b.x > 0 && b.y > 0)
                go.transform.localScale = new Vector3(cellSize / b.x, cellSize / b.y, 1);
            else
                go.transform.localScale = Vector3.one * cellSize;
        }
        else
        {
            go.transform.localScale = Vector3.one * cellSize;
        }

        // Resize collider to cover 95% of cell in world space, regardless of sprite scale
        var col2D = go.GetComponent<Collider2D>();
        if (col2D is BoxCollider2D bc)
        {
            float sX = Mathf.Abs(go.transform.localScale.x);
            float sY = Mathf.Abs(go.transform.localScale.y);
            if (sX > 0 && sY > 0)
                bc.size = new Vector2(cellSize * 0.95f / sX, cellSize * 0.95f / sY);
        }

        obj = go.GetComponent<GridObject>();
        if (obj == null) obj = go.AddComponent<GridObject>();
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

        // Keep collider for physics, hide the visual
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
    }

    /// <summary>
    /// Loads and places the level background image behind the game area.
    /// </summary>
    private void CreateLevelBackground(Transform parent)
    {
        // Try loading level-specific background
        string bgPath = "UI/Sprites/Levels/l" + levelIndex;
        var bgTex = Resources.Load<Texture2D>(bgPath);
        if (bgTex == null)
        {
            bgPath = "UI/Sprites/Levels/01";
            bgTex = Resources.Load<Texture2D>(bgPath);
        }

        if (bgTex == null) return;

        var bgSprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height),
            new Vector2(0.5f, 0.5f), Mathf.Max(bgTex.width, bgTex.height));

        var bgGo = new GameObject("LevelBackground");
        bgGo.transform.SetParent(parent, false);
        bgGo.transform.position = new Vector3(0, GroundTopY + rows * cellSize / 2f, 5);
        bgGo.transform.localScale = new Vector3(
            Mathf.Max(cols * cellSize * 3f, 30f),
            Mathf.Max(rows * cellSize * 3f, 20f),
            1);

        var sr = bgGo.AddComponent<SpriteRenderer>();
        sr.sprite = bgSprite;
        sr.color = new Color(0.6f, 0.6f, 0.6f, 0.4f);
        sr.sortingOrder = -3;

        Debug.Log("[SceneBuilder] Level background loaded: " + bgPath);
    }

    /// <summary>
    /// Loads sky and sun decorations.
    /// </summary>
    private void CreateSky(Transform parent)
    {
        // Sky — large background plane behind everything
        var skyTex = Resources.Load<Texture2D>("UI/Sprites/Objects/sky1");
        if (skyTex != null)
        {
            var skySprite = Sprite.Create(skyTex, new Rect(0, 0, skyTex.width, skyTex.height),
                new Vector2(0.5f, 0.5f), Mathf.Max(skyTex.width, skyTex.height));

            var skyGo = new GameObject("Sky");
            skyGo.transform.SetParent(parent, false);
            skyGo.transform.position = new Vector3(0, GroundTopY + rows * cellSize / 2f, 10);
            skyGo.transform.localScale = new Vector3(
                Mathf.Max(cols * cellSize * 4f, 40f),
                Mathf.Max(rows * cellSize * 4f, 25f),
                1);

            var sr = skyGo.AddComponent<SpriteRenderer>();
            sr.sprite = skySprite;
            sr.color = new Color(1f, 1f, 1f, 0.9f);
            sr.sortingOrder = -10;
        }

        // Sun
        var sunTex = Resources.Load<Texture2D>("UI/Sprites/Objects/sun");
        if (sunTex != null)
        {
            var sunSprite = Sprite.Create(sunTex, new Rect(0, 0, sunTex.width, sunTex.height),
                new Vector2(0.5f, 0.5f), Mathf.Max(sunTex.width, sunTex.height));

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.position = new Vector3(
                -cols * cellSize / 2f - 3f,
                GroundTopY + rows * cellSize + 3f,
                8);
            sunGo.transform.localScale = new Vector3(3, 3, 1);

            var sr = sunGo.AddComponent<SpriteRenderer>();
            sr.sprite = sunSprite;
            sr.color = Color.white;
            sr.sortingOrder = -8;
        }

        // Clouds
        var cloudTex = Resources.Load<Texture2D>("UI/Sprites/Objects/cloud");
        if (cloudTex != null)
        {
            var cloudSprite = Sprite.Create(cloudTex, new Rect(0, 0, cloudTex.width, cloudTex.height),
                new Vector2(0.5f, 0.5f), Mathf.Max(cloudTex.width, cloudTex.height));

            for (int i = 0; i < 3; i++)
            {
                var cloudGo = new GameObject("Cloud_" + i);
                cloudGo.transform.SetParent(parent, false);
                cloudGo.transform.position = new Vector3(
                    Random.Range(-cols * cellSize / 2f, cols * cellSize / 2f),
                    GroundTopY + rows * cellSize + Random.Range(2f, 6f),
                    7);
                cloudGo.transform.localScale = new Vector3(4, 2, 1);

                var sr = cloudGo.AddComponent<SpriteRenderer>();
                sr.sprite = cloudSprite;
                sr.color = new Color(1, 1, 1, 0.8f);
                sr.sortingOrder = -7;
            }
        }
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
                    // Alternating subtle tones with slight transparency
                    cell.backgroundRenderer.color = (r + c) % 2 == 0
                        ? new Color(1f, 1f, 1f, 0.08f)
                        : new Color(0f, 0f, 0f, 0.10f);
                }
            }
        }

        CreateGridBorder();
    }

    /// <summary>
    /// Draws a thin semi-transparent border frame around the entire grid area.
    /// </summary>
    private void CreateGridBorder()
    {
        var border = new GameObject("GridBorder");
        border.transform.SetParent(GridSystem.transform.parent, false);

        float gridW = cols * cellSize;
        float gridH = rows * cellSize;
        Vector2 origin = GridSystem.gridOrigin;
        float cx = origin.x + gridW * 0.5f - cellSize * 0.5f;
        float cy = origin.y + gridH * 0.5f - cellSize * 0.5f;
        var borderColor = new Color(1f, 1f, 1f, 0.25f);
        float thickness = 0.08f;

        // Top edge
        CreateEdge(border.transform, "Edge_Top",
            new Vector3(cx, origin.y + rows * cellSize - cellSize * 0.5f, 0),
            new Vector2(gridW + cellSize, thickness), borderColor);
        // Bottom edge
        CreateEdge(border.transform, "Edge_Bottom",
            new Vector3(cx, origin.y - cellSize * 0.5f, 0),
            new Vector2(gridW + cellSize, thickness), borderColor);
        // Left edge
        CreateEdge(border.transform, "Edge_Left",
            new Vector3(origin.x - cellSize * 0.5f, cy, 0),
            new Vector2(thickness, gridH + cellSize), borderColor);
        // Right edge
        CreateEdge(border.transform, "Edge_Right",
            new Vector3(origin.x + cols * cellSize - cellSize * 0.5f, cy, 0),
            new Vector2(thickness, gridH + cellSize), borderColor);

        // Subtle corner accents
        float cornerSize = cellSize * 0.5f;
        Vector3 tl = new Vector3(origin.x - cellSize * 0.5f, origin.y + rows * cellSize - cellSize * 0.5f, 0);
        Vector3 tr = new Vector3(origin.x + cols * cellSize - cellSize * 0.5f, origin.y + rows * cellSize - cellSize * 0.5f, 0);
        Vector3 bl = new Vector3(origin.x - cellSize * 0.5f, origin.y - cellSize * 0.5f, 0);
        Vector3 br = new Vector3(origin.x + cols * cellSize - cellSize * 0.5f, origin.y - cellSize * 0.5f, 0);

        CreateEdge(border.transform, "Corner_TL", tl, new Vector2(cornerSize, thickness * 2), borderColor);
        CreateEdge(border.transform, "Corner_TL_V", tl, new Vector2(thickness * 2, cornerSize), borderColor);
        CreateEdge(border.transform, "Corner_TR", tr, new Vector2(cornerSize, thickness * 2), borderColor);
        CreateEdge(border.transform, "Corner_TR_V", tr, new Vector2(thickness * 2, cornerSize), borderColor);
        CreateEdge(border.transform, "Corner_BL", bl, new Vector2(cornerSize, thickness * 2), borderColor);
        CreateEdge(border.transform, "Corner_BL_V", bl, new Vector2(thickness * 2, cornerSize), borderColor);
        CreateEdge(border.transform, "Corner_BR", br, new Vector2(cornerSize, thickness * 2), borderColor);
        CreateEdge(border.transform, "Corner_BR_V", br, new Vector2(thickness * 2, cornerSize), borderColor);
    }

    private void CreateEdge(Transform parent, string name, Vector3 pos, Vector2 scale, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSprite;
        sr.color = color;
        sr.sortingOrder = 1;
    }

    private void CreateInitialObjects()
    {
        // No initial objects — all spawned via BuildSystem
    }

    /// <summary>
    /// Places bedrock blocks randomly on left and right edges of the grid.
    /// </summary>
    private void CreateBedrock()
    {
        int edgeColCount = Mathf.Max(1, rows / 3); // ~1/3 of edge rows get bedrock

        // Left edge (col 0)
        for (int i = 0; i < edgeColCount; i++)
        {
            int r = Random.Range(0, rows);
            if (GridSystem.GetCell(r, 0) != null && GridSystem.GetCell(r, 0).IsEmpty)
            {
                var obj = CreateObject(ObjectType.Bedrock, "基岩", ObjectConfig.Get(ObjectType.Bedrock).color,
                    GridSystem.GridToWorldPosition(r, 0));
                obj.PlaceOnGrid(GridSystem, r, 0);
            }
        }

        // Right edge (col cols-1)
        for (int i = 0; i < edgeColCount; i++)
        {
            int r = Random.Range(0, rows);
            if (GridSystem.GetCell(r, cols - 1) != null && GridSystem.GetCell(r, cols - 1).IsEmpty)
            {
                var obj = CreateObject(ObjectType.Bedrock, "基岩", ObjectConfig.Get(ObjectType.Bedrock).color,
                    GridSystem.GridToWorldPosition(r, cols - 1));
                obj.PlaceOnGrid(GridSystem, r, cols - 1);
            }
        }

        Debug.Log($"[SceneBuilder] Bedrock placed on edges ({edgeColCount * 2} max)");
    }

    /// <summary>
    /// Randomly places balloons in the interior of the grid (not on edges).
    /// </summary>
    private void CreateBalloons()
    {
        for (int i = 0; i < balloonCount; i++)
        {
            int attempts = 0;
            int maxAttempts = rows * cols;
            int r, c;

            do
            {
                // At least 2 cells away from any edge
                r = Random.Range(2, Mathf.Max(3, rows - 2));
                c = Random.Range(2, Mathf.Max(3, cols - 2));
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
