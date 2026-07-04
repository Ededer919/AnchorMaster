using System;
using UnityEngine;

/// <summary>
/// Grid object that snaps to grid cells. One cell can only hold one GridObject at a time.
/// </summary>
public class GridObject : MonoBehaviour
{
    [Header("Identity")]
    public string objectID = "";
    public string displayName = "";

    [Header("Type")]
    public ObjectType objectType = ObjectType.Plank;
    public float mass = 1f;

    [Header("Snap Settings")]
    [Range(1f, 50f)] public float snapSpeed = 15f;
    public bool snapOnStart = false;
    public Vector3 offset = Vector3.zero;

    [Header("Wind")]
    public bool receiveWind = false;
    [Range(0f, 1f)] public float windSensitivity = 0.3f;
    [Range(0f, 5f)] public float windSwaySpeed = 3f;

    [Header("Visual")]
    public SpriteRenderer highlightRenderer;
    protected Color normalColor = Color.white;
    protected Color highlightColor = new Color(1f, 1f, 0.4f, 1f);

    public GridCell CurrentCell { get; private set; }
    public bool IsPlaced => CurrentCell != null;
    public int Row => CurrentCell != null ? CurrentCell.Row : -1;
    public int Col => CurrentCell != null ? CurrentCell.Col : -1;

    public bool IsDragging { get; set; }

    // Physics mode: when true, wind sway Lerp is disabled, parent Rigidbody2D drives motion
    public bool PhysicsMode { get; private set; }

    public event Action<GridCell> OnPlaced;
    public event Action<GridCell> OnRemoved;
    public event Action<GridCell, GridCell> OnMoved;

    private Vector3 targetPosition;
    private bool hasTarget;
    private SpriteRenderer mainRenderer;
    private Vector3 windOffset;
    private Vector3 windOffsetVelocity;

    private void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer != null)
            normalColor = mainRenderer.color;
    }

    private void Start()
    {
        if (snapOnStart)
        {
            var grid = FindFirstObjectByType<GridSystem>();
            if (grid != null)
            {
                Vector2Int pos = grid.WorldToGridPosition(transform.position);
                PlaceOnGrid(grid, pos.y, pos.x);
            }
        }
    }

    private void Update()
    {
        if (PhysicsMode)
            return; // Parent Rigidbody2D drives motion

        // Calculate wind offset (visual sway, does not affect grid occupation)
        if (receiveWind && !IsDragging && WindSystem2D.Instance != null)
        {
            Vector2 wind = WindSystem2D.Instance.GetWindForceAt(transform.position);
            Vector3 targetWindOffset = (Vector3)wind * windSensitivity * 0.1f;
            windOffset = Vector3.SmoothDamp(windOffset, targetWindOffset, ref windOffsetVelocity, 1f / windSwaySpeed);
        }
        else
        {
            windOffset = Vector3.SmoothDamp(windOffset, Vector3.zero, ref windOffsetVelocity, 0.2f);
        }

        if (hasTarget && !IsDragging)
        {
            Vector3 finalTarget = targetPosition + windOffset;
            transform.position = Vector3.Lerp(transform.position, finalTarget, Time.deltaTime * snapSpeed);
            if (Vector3.Distance(transform.position, finalTarget) < 0.01f)
            {
                transform.position = finalTarget;
                hasTarget = false;
            }
        }
        else if (!IsDragging && IsPlaced)
        {
            // Apply wind sway even when already snapped
            transform.position = targetPosition + windOffset;
        }
    }

    /// <summary>
    /// Places this object onto the specified cell of the grid. Fails if cell is occupied by another object.
    /// </summary>
    public bool PlaceOnGrid(GridSystem grid, int row, int col)
    {
        if (grid == null)
            return false;

        GridCell cell = grid.GetCell(row, col);
        if (cell == null)
            return false;

        if (!cell.IsEmpty && cell.OccupiedObject != this)
            return false;

        GridCell oldCell = CurrentCell;
        if (oldCell != null && oldCell != cell)
        {
            oldCell.OccupiedObject = null;
            OnRemoved?.Invoke(oldCell);
        }

        CurrentCell = cell;
        cell.OccupiedObject = this;

        targetPosition = cell.transform.position + offset;
        hasTarget = true;

        if (oldCell != null && oldCell != cell)
            OnMoved?.Invoke(oldCell, cell);
        else
            OnPlaced?.Invoke(cell);

        return true;
    }

    /// <summary>
    /// Places this object onto the nearest empty cell to the given world position.
    /// </summary>
    public bool PlaceAtNearest(GridSystem grid, Vector2 worldPos)
    {
        if (grid == null)
            return false;

        Vector2Int gridPos = grid.WorldToGridPosition(worldPos);
        return PlaceOnGrid(grid, gridPos.y, gridPos.x);
    }

    /// <summary>
    /// Removes this object from its current cell.
    /// </summary>
    public void RemoveFromGrid()
    {
        if (CurrentCell == null)
            return;

        GridCell oldCell = CurrentCell;
        oldCell.OccupiedObject = null;
        CurrentCell = null;
        hasTarget = false;
        OnRemoved?.Invoke(oldCell);
    }

    /// <summary>
    /// Moves to the target cell. If occupied, swaps positions with the other object.
    /// </summary>
    public bool MoveTo(GridSystem grid, int row, int col)
    {
        if (grid == null)
            return false;

        GridCell target = grid.GetCell(row, col);
        if (target == null)
            return false;

        if (target.IsEmpty || target.OccupiedObject == this)
        {
            return PlaceOnGrid(grid, row, col);
        }

        // Swap with the occupying object
        GridObject other = target.OccupiedObject;
        GridCell myOldCell = CurrentCell;

        if (myOldCell != null)
            myOldCell.OccupiedObject = null;
        target.OccupiedObject = null;

        if (myOldCell != null)
        {
            other.CurrentCell = myOldCell;
            myOldCell.OccupiedObject = other;
            other.targetPosition = myOldCell.transform.position + other.offset;
            other.hasTarget = true;
            other.OnMoved?.Invoke(target, myOldCell);
        }

        CurrentCell = target;
        target.OccupiedObject = this;
        targetPosition = target.transform.position + offset;
        hasTarget = true;
        OnMoved?.Invoke(myOldCell, target);

        return true;
    }

    /// <summary>
    /// Toggles highlight visual on this object.
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.gameObject.SetActive(highlight);
            if (highlight)
                highlightRenderer.color = highlightColor;
        }

        if (mainRenderer != null)
        {
            mainRenderer.color = highlight ? highlightColor : normalColor;
        }
    }

    /// <summary>
    /// Returns the current wind force at this object's position. Returns Vector2.zero if wind is disabled or no WindSystem2D exists.
    /// </summary>
    public Vector2 GetWindForceAt()
    {
        if (!receiveWind || WindSystem2D.Instance == null)
            return Vector2.zero;
        return WindSystem2D.Instance.GetWindForceAt(transform.position);
    }

    // ========== Physics Mode ==========

    /// <summary>
    /// Switches to physics mode: disables wind sway Lerp, parent Rigidbody2D drives motion.
    /// </summary>
    public void EnablePhysicsMode()
    {
        PhysicsMode = true;
        hasTarget = false;
    }

    /// <summary>
    /// Switches back to normal mode: wind sway Lerp resumes.
    /// </summary>
    public void DisablePhysicsMode()
    {
        PhysicsMode = false;
        windOffset = Vector3.zero;
        hasTarget = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (CurrentCell != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(CurrentCell.transform.position, Vector3.one * 0.9f);

            // Draw wind offset line if receiving wind
            if (receiveWind && WindSystem2D.Instance != null)
            {
                Vector2 wind = WindSystem2D.Instance.GetWindForceAt(transform.position);
                if (wind.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(CurrentCell.transform.position, transform.position);
                }
            }
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
        }
    }

    private void OnDestroy()
    {
        if (CurrentCell != null && CurrentCell.OccupiedObject == this)
        {
            CurrentCell.OccupiedObject = null;
        }
    }
}
