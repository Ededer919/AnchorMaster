using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles mouse drag input for GridObjects. Supports grid snap and free build modes.
/// </summary>
public class GridInputHandler : MonoBehaviour
{
    private GridSystem gridSystem;
    private GridObjectManager objectManager;
    private BuildSystem buildSystem;
    private float groundTopY;
    private float cellSize;
    private bool inputSubscribed;
    private bool wantInput;

    public void Initialize(GridSystem grid, GridObjectManager manager, float groundY, float cellSz)
    {
        gridSystem = grid;
        objectManager = manager;
        groundTopY = groundY;
        cellSize = cellSz;
    }

    public void SetBuildSystem(BuildSystem build)
    {
        buildSystem = build;
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

    private GridObject dragging;
    private Vector3 dragOffset;
    private bool buildMode;

    public void SetBuildMode(bool mode)
    {
        buildMode = mode;
    }

    private void OnClickDown(Vector2 screenPos)
    {
        var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es != null && es.IsPointerOverGameObject())
            return;

        Vector2 pos = Camera.main.ScreenToWorldPoint(screenPos);
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);

        GridObject target = null;
        foreach (var hit in hits)
        {
            target = hit.GetComponent<GridObject>();
            if (target != null && target.objectType != ObjectType.Balloon) break;
            target = null;
        }

        if (target != null)
        {
            dragging = target;
            dragging.IsDragging = true;
            dragOffset = (Vector3)pos - dragging.transform.position;

            if (dragging.IsPlaced)
                dragging.RemoveFromGrid();

            buildSystem?.OnObjectPickedUp(dragging);

            if (!buildMode)
            {
                gridSystem.HighlightValidCells(true);
                foreach (var cell in gridSystem.GetAllCells())
                {
                    if (cell != null && !cell.IsEmpty)
                        cell.SetHighlighted(false);
                }
            }

            Debug.Log($"[Input] Picked up: {dragging.displayName}");
        }
    }

    private void OnClickHold(Vector2 screenPos)
    {
        if (dragging == null) return;

        Vector2 pos = Camera.main.ScreenToWorldPoint(screenPos);
        dragging.transform.position = (Vector3)pos - dragOffset;

        if (!buildMode)
        {
            Vector2Int nearest = objectManager.FindNearestEmptyCell(pos);
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

        Vector2 pos = Camera.main.ScreenToWorldPoint(screenPos);
        dragging.IsDragging = false;

        if (buildMode)
        {
            float clampedY = Mathf.Max(pos.y, groundTopY + cellSize * 0.5f);
            dragging.transform.position = new Vector3(pos.x, clampedY, 0);
            Debug.Log($"[Input] Free placed {dragging.displayName} at ({pos.x:F1}, {clampedY:F1})");
            UIManager.Instance?.UpdateStatus($"{dragging.displayName} placed at ({pos.x:F1}, {clampedY:F1})");
        }
        else
        {
            Vector2Int cell = objectManager.FindNearestEmptyCell(pos);
            if (cell.x >= 0)
            {
                bool placed = dragging.PlaceOnGrid(gridSystem, cell.y, cell.x);
                if (placed)
                {
                    buildSystem?.OnObjectPlaced(dragging);
                    Debug.Log($"[Input] Snapped {dragging.displayName} to ({cell.y}, {cell.x})");
                    UIManager.Instance?.UpdateStatus($"{dragging.displayName} at grid ({cell.y}, {cell.x})");
                }
            }
            gridSystem.HighlightValidCells(false);
        }

        dragging = null;
    }

    private void OnDestroy()
    {
        DisableInput();
    }
}
