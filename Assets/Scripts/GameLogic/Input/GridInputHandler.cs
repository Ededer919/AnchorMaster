using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse drag input. Supports:
/// 1. Dragging existing GridObjects on the grid
/// 2. Dragging new objects from the UI toolbar (via BuildSystem.BeginDragFromToolbar)
/// </summary>
public class GridInputHandler : MonoBehaviour
{
    private GridSystem gridSystem;
    private GridObjectManager objectManager;
    private BuildSystem buildSystem;
    private GameUIBuilder uiBuilder;
    private Camera cam;
    private float groundTopY;
    private float cellSize;
    private bool inputSubscribed;
    private bool wantInput;
    private int gameLayerMask = 1 << 8; // Game layer

    // Drag state
    private GridObject dragging;
    private Vector3 dragOffset;
    private bool fromToolbar;

    public void Initialize(GridSystem grid, GridObjectManager manager, float groundY, float cellSz)
    {
        gridSystem = grid;
        objectManager = manager;
        groundTopY = groundY;
        cellSize = cellSz;
        cam = Camera.main;
    }

    public void SetBuildSystem(BuildSystem build)
    {
        buildSystem = build;
    }

    public void SetUIBuilder(GameUIBuilder ui)
    {
        uiBuilder = ui;
    }

    public void EnableInput()
    {
        wantInput = true;
        TrySubscribe();
    }

    public void DisableInput()
    {
        wantInput = false;
        if (InputManager.Instance != null && inputSubscribed)
        {
            InputManager.Instance.OnClickDown -= OnClickDown;
            InputManager.Instance.OnClickHold -= OnClickHold;
            InputManager.Instance.OnClickUp -= OnClickUp;
            inputSubscribed = false;
        }
    }

    private void Update()
    {
        if (wantInput && !inputSubscribed)
            TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (InputManager.Instance != null && !inputSubscribed)
        {
            InputManager.Instance.OnClickDown += OnClickDown;
            InputManager.Instance.OnClickHold += OnClickHold;
            InputManager.Instance.OnClickUp += OnClickUp;
            inputSubscribed = true;
            Debug.Log("[Input] Subscribed to InputManager.");
        }
    }

    private void OnClickDown(Vector2 screenPos)
    {
        var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es != null && es.IsPointerOverGameObject())
            return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
        Physics2D.SyncTransforms();
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos, gameLayerMask);

        // Find a draggable GridObject (skip balloons)
        GridObject target = null;
        foreach (var hit in hits)
        {
            target = hit.GetComponent<GridObject>();
            if (target != null && target.objectType != ObjectType.Balloon && target.objectType != ObjectType.Bedrock)
                break;
            target = null;
        }

        // Fallback: if no collider hit, find the closest draggable object within half a cell.
        // Catches objects that are mid-Lerp (collider between cells) or click in collider dead zone.
        if (target == null)
        {
            float maxDist = cellSize * 0.75f;
            float closestDist = maxDist;
            foreach (var obj in objectManager.GetAllObjects())
            {
                if (obj == null || obj.objectType == ObjectType.Balloon || obj.objectType == ObjectType.Bedrock)
                    continue;
                float dist = Vector2.Distance(obj.transform.position, worldPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = obj;
                }
            }
        }

        if (target != null)
        {
            // Drag existing object
            dragging = target;
            dragging.IsDragging = true;
            dragOffset = (Vector3)worldPos - dragging.transform.position;
            fromToolbar = false;

            if (dragging.IsPlaced)
                dragging.RemoveFromGrid();

            buildSystem?.OnObjectPickedUp(dragging);

            if (!buildMode)
            {
                gridSystem.HighlightValidCells(true);
                foreach (var cell in gridSystem.GetAllCells())
                {
                    if (cell != null && !gridSystem.CanPlaceAt(cell.Row, cell.Col))
                        cell.SetHighlighted(false);
                }
            }

            Debug.Log($"[Input] Picked up: {dragging.displayName}");
        }
    }

    private void OnClickHold(Vector2 screenPos)
    {
        if (dragging == null) return;

        Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
        dragging.transform.position = (Vector3)worldPos - dragOffset;

        if (!buildMode && !fromToolbar)
        {
            Vector2Int nearest = objectManager.FindNearestEmptyCell(worldPos);
            if (nearest.x >= 0)
            {
                gridSystem.HighlightValidCells(false);
                GridCell targetCell = gridSystem.GetCell(nearest.y, nearest.x);
                if (targetCell != null && targetCell.IsEmpty)
                    targetCell.SetHighlighted(true);
            }
        }
    }

    private void OnClickUp(Vector2 screenPos)
    {
        if (dragging == null) return;

        Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
        dragging.IsDragging = false;

        if (buildMode)
        {
            float clampedY = Mathf.Max(worldPos.y, groundTopY + cellSize * 0.5f);
            dragging.transform.position = new Vector3(worldPos.x, clampedY, 0);
            buildSystem?.OnObjectPlaced(dragging);
            UIManager.Instance?.UpdateStatus($"{dragging.displayName} 已放置");
            PlayPlacementEffect(dragging);
        }
        else
        {
            Vector2Int cell = objectManager.FindNearestEmptyCell(worldPos);
            if (cell.x >= 0)
            {
                bool placed = dragging.PlaceOnGrid(gridSystem, cell.y, cell.x);
                if (placed)
                {
                    buildSystem?.OnObjectPlaced(dragging);
                    UIManager.Instance?.UpdateStatus($"{dragging.displayName} 放置在 ({cell.y},{cell.x})");
                    PlayPlacementEffect(dragging);
                }
            }
            else
            {
                // No valid cell — if from toolbar, cancel (refund); if existing, leave in place
                if (fromToolbar)
                {
                    buildSystem?.CancelDrag(dragging);
                }
            }
            gridSystem.HighlightValidCells(false);
        }

        dragging = null;
        fromToolbar = false;
    }

    private bool buildMode;
    public void SetBuildMode(bool mode) { buildMode = mode; }

    /// <summary>
    /// Called by GameUIBuilder when a toolbar card is clicked.
    /// Creates a new object and starts dragging it.
    /// </summary>
    public bool StartToolbarDrag(ObjectType type, Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);

        var obj = buildSystem?.BeginDragFromToolbar(type, worldPos);
        if (obj == null) return false;

        dragging = obj;
        dragging.IsDragging = true;
        dragOffset = Vector3.zero;
        fromToolbar = true;

        PlayPlacementEffect(dragging);

        return true;
    }

    /// <summary>
    /// Plays the block placement particle effect and sound at the object's position.
    /// </summary>
    private void PlayPlacementEffect(GridObject obj)
    {
        if (BlockPlacementEffect.Instance == null) return;
        Color color = ObjectConfig.Get(obj.objectType).color;
        BlockPlacementEffect.Instance.PlayEffect(obj.transform.position, color);
        AudioManager.Instance?.PlayPut();
    }

    private void OnDestroy()
    {
        DisableInput();
    }
}
